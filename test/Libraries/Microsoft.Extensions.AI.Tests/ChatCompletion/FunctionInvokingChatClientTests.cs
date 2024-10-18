// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class FunctionInvokingChatClientTests
{
    [Fact]
    public void Ctor_HasExpectedDefaults()
    {
        using TestChatClient innerClient = new();
        using FunctionInvokingChatClient client = new(innerClient);

        Assert.False(client.ConcurrentInvocation);
        Assert.False(client.DetailedErrors);
        Assert.True(client.KeepFunctionCallingMessages);
        Assert.Null(client.MaximumIterationsPerRequest);
        Assert.False(client.RetryOnError);
    }

    [Fact]
    public async Task SupportsSingleFunctionCallPerRequestAsync()
    {
        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((int i) => { }, "VoidReturn"),
            ]
        };

        await InvokeAndAssertAsync(options, [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportsMultipleFunctionCallsPerRequestAsync(bool concurrentInvocation)
    {
        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create((int i) => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };
        await InvokeAndAssertAsync(options, [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("callId1", "Func1"),
                new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 34 } }),
                new FunctionCallContent("callId3", "Func2", arguments: new Dictionary<string, object?> { { "i", 56 } }),
            ]),
            new ChatMessage(ChatRole.Tool, [
                new FunctionResultContent("callId1", "Func1", result: "Result 1"),
                new FunctionResultContent("callId2", "Func2", result: "Result 2: 34"),
                new FunctionResultContent("callId3", "Func2", result: "Result 2: 56"),
            ]),
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("callId4", "Func2", arguments: new Dictionary<string, object?> { { "i", 78 } }),
                new FunctionCallContent("callId5", "Func1")]),
            new ChatMessage(ChatRole.Tool, [
                new FunctionResultContent("callId4", "Func2", result: "Result 2: 78"),
                new FunctionResultContent("callId5", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ], configurePipeline: b => b.Use(s => new FunctionInvokingChatClient(s) { ConcurrentInvocation = concurrentInvocation }));
    }

    [Fact]
    public async Task ParallelFunctionCallsMayBeInvokedConcurrentlyAsync()
    {
        using var barrier = new Barrier(2);

        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create((string arg) =>
                {
                    barrier.SignalAndWait();
                    return arg + arg;
                }, "Func"),
            ]
        };

        await InvokeAndAssertAsync(options, [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("callId1", "Func", arguments: new Dictionary<string, object?> { { "arg", "hello" } }),
                new FunctionCallContent("callId2", "Func", arguments: new Dictionary<string, object?> { { "arg", "world" } }),
            ]),
            new ChatMessage(ChatRole.Tool, [
                new FunctionResultContent("callId1", "Func", result: "hellohello"),
                new FunctionResultContent("callId2", "Func", result: "worldworld"),
            ]),
            new ChatMessage(ChatRole.Assistant, "done"),
        ], configurePipeline: b => b.Use(s => new FunctionInvokingChatClient(s) { ConcurrentInvocation = true }));
    }

    [Fact]
    public async Task ConcurrentInvocationOfParallelCallsDisabledByDefaultAsync()
    {
        int activeCount = 0;

        var options = new ChatOptions
        {
            Tools = [
                AIFunctionFactory.Create(async (string arg) =>
                {
                    Interlocked.Increment(ref activeCount);
                    await Task.Delay(100);
                    Assert.Equal(1, activeCount);
                    Interlocked.Decrement(ref activeCount);
                    return arg + arg;
                }, "Func"),
            ]
        };

        await InvokeAndAssertAsync(options, [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("callId1", "Func", arguments: new Dictionary<string, object?> { { "arg", "hello" } }),
                new FunctionCallContent("callId2", "Func", arguments: new Dictionary<string, object?> { { "arg", "world" } }),
            ]),
            new ChatMessage(ChatRole.Tool, [
                new FunctionResultContent("callId1", "Func", result: "hellohello"),
                new FunctionResultContent("callId2", "Func", result: "worldworld"),
            ]),
            new ChatMessage(ChatRole.Assistant, "done"),
        ]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovesFunctionCallingMessagesWhenRequestedAsync(bool keepFunctionCallingMessages)
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((int i) => { }, "VoidReturn"),
            ]
        };

#pragma warning disable SA1118 // Parameter should not span multiple lines
        var finalChat = await InvokeAndAssertAsync(
            options,
            [
                new ChatMessage(ChatRole.User, "hello"),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: "Result 1")]),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
                new ChatMessage(ChatRole.Assistant, "world"),
            ],
            expected: keepFunctionCallingMessages ?
                null :
                [
                    new ChatMessage(ChatRole.User, "hello"),
                    new ChatMessage(ChatRole.Assistant, "world")
                ],
            configurePipeline: b => b.Use(client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = keepFunctionCallingMessages }));
#pragma warning restore SA1118

        IEnumerable<AIContent> content = finalChat.SelectMany(m => m.Contents);
        if (keepFunctionCallingMessages)
        {
            Assert.Contains(content, c => c is FunctionCallContent or FunctionResultContent);
        }
        else
        {
            Assert.All(content, c => Assert.False(c is FunctionCallContent or FunctionResultContent));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovesFunctionCallingContentWhenRequestedAsync(bool keepFunctionCallingMessages)
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((int i) => { }, "VoidReturn"),
            ]
        };

#pragma warning disable SA1118 // Parameter should not span multiple lines
        var finalChat = await InvokeAndAssertAsync(options,
            [
                new ChatMessage(ChatRole.User, "hello"),
                new ChatMessage(ChatRole.Assistant, [new TextContent("extra"), new FunctionCallContent("callId1", "Func1"), new TextContent("stuff")]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func1", result: "Result 1")]),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } }), new TextContent("more")]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
                new ChatMessage(ChatRole.Assistant, "world"),
            ],
            expected: keepFunctionCallingMessages ?
                null :
                [
                    new ChatMessage(ChatRole.User, "hello"),
                    new ChatMessage(ChatRole.Assistant, [new TextContent("extra"), new TextContent("stuff")]),
                    new ChatMessage(ChatRole.Assistant, "more"),
                    new ChatMessage(ChatRole.Assistant, "world"),
                ],
        configurePipeline: b => b.Use(client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = keepFunctionCallingMessages }));
#pragma warning restore SA1118

        IEnumerable<AIContent> content = finalChat.SelectMany(m => m.Contents);
        if (keepFunctionCallingMessages)
        {
            Assert.Contains(content, c => c is FunctionCallContent or FunctionResultContent);
        }
        else
        {
            Assert.All(content, c => Assert.False(c is FunctionCallContent or FunctionResultContent));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExceptionDetailsOnlyReportedWhenRequestedAsync(bool detailedErrors)
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(string () => throw new InvalidOperationException("Oh no!"), "Func1"),
            ]
        };

        await InvokeAndAssertAsync(options, [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: detailedErrors ? "Error: Function failed. Exception: Oh no!" : "Error: Function failed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ], configurePipeline: b => b.Use(s => new FunctionInvokingChatClient(s) { DetailedErrors = detailedErrors }));
    }

    [Fact]
    public async Task RejectsMultipleChoicesAsync()
    {
        var func1 = AIFunctionFactory.Create(() => "Some result 1", "Func1");
        var func2 = AIFunctionFactory.Create(() => "Some result 2", "Func2");

        using var innerClient = new TestChatClient
        {
            CompleteAsyncCallback = async (chatContents, options, cancellationToken) =>
            {
                await Task.Yield();

                return new ChatCompletion(
                [
                    new(ChatRole.Assistant, [new FunctionCallContent("callId1", func1.Metadata.Name)]),
                    new(ChatRole.Assistant, [new FunctionCallContent("callId2", func2.Metadata.Name)]),
                ]);
            }
        };

        IChatClient service = new ChatClientBuilder().UseFunctionInvocation().Use(innerClient);

        List<ChatMessage> chat = [new ChatMessage(ChatRole.User, "hello")];
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CompleteAsync(chat, new ChatOptions { Tools = [func1, func2] }));

        Assert.Contains("only accepts a single choice", ex.Message);
        Assert.Single(chat); // It didn't add anything to the chat history
    }

    private static async Task<List<ChatMessage>> InvokeAndAssertAsync(
        ChatOptions options,
        List<ChatMessage> plan,
        List<ChatMessage>? expected = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null)
    {
        Assert.NotEmpty(plan);

        configurePipeline ??= static b => b.UseFunctionInvocation();

        using CancellationTokenSource cts = new();
        List<ChatMessage> chat = [plan[0]];
        int i = 0;

        using var innerClient = new TestChatClient
        {
            CompleteAsyncCallback = async (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Same(chat, contents);
                Assert.Equal(cts.Token, actualCancellationToken);

                await Task.Yield();

                return new ChatCompletion([plan[contents.Count]]);
            }
        };

        IChatClient service = configurePipeline(new ChatClientBuilder()).Use(innerClient);

        var result = await service.CompleteAsync(chat, options, cts.Token);
        chat.Add(result.Message);

        expected ??= plan;
        Assert.NotNull(result);
        Assert.Equal(expected.Count, chat.Count);
        for (; i < expected.Count; i++)
        {
            var expectedMessage = expected[i];
            var chatMessage = chat[i];

            Assert.Equal(expectedMessage.Role, chatMessage.Role);
            Assert.Equal(expectedMessage.Text, chatMessage.Text);
            Assert.Equal(expectedMessage.GetType(), chatMessage.GetType());

            Assert.Equal(expectedMessage.Contents.Count, chatMessage.Contents.Count);
            for (int j = 0; j < expectedMessage.Contents.Count; j++)
            {
                var expectedItem = expectedMessage.Contents[j];
                var chatItem = chatMessage.Contents[j];

                Assert.Equal(expectedItem.GetType(), chatItem.GetType());
                Assert.Equal(expectedItem.ToString(), chatItem.ToString());
                if (expectedItem is FunctionCallContent expectedFunctionCall)
                {
                    var chatFunctionCall = (FunctionCallContent)chatItem;
                    Assert.Equal(expectedFunctionCall.Name, chatFunctionCall.Name);
                    AssertExtensions.EqualFunctionCallParameters(expectedFunctionCall.Arguments, chatFunctionCall.Arguments);
                }
                else if (expectedItem is FunctionResultContent expectedFunctionResult)
                {
                    var chatFunctionResult = (FunctionResultContent)chatItem;
                    AssertExtensions.EqualFunctionCallResults(expectedFunctionResult.Result, chatFunctionResult.Result);
                }
            }
        }

        return chat;
    }
}
