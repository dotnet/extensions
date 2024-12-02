// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Trace;
using Xunit;

#pragma warning disable SA1118 // Parameter should not span multiple lines

namespace Microsoft.Extensions.AI;

public class FunctionInvokingChatClientTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new FunctionInvokingChatClient(null!));
        Assert.Throws<ArgumentNullException>("builder", () => ((ChatClientBuilder)null!).UseFunctionInvocation());
    }

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
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((int i) => { }, "VoidReturn"),
            ]
        };

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, plan);

        await InvokeAndAssertStreamingAsync(options, plan);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportsMultipleFunctionCallsPerRequestAsync(bool concurrentInvocation)
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create((int i) => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "Func1"),
                new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 34 } }),
                new FunctionCallContent("callId3", "Func2", arguments: new Dictionary<string, object?> { { "i", 56 } }),
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", "Func1", result: "Result 1"),
                new FunctionResultContent("callId2", "Func2", result: "Result 2: 34"),
                new FunctionResultContent("callId3", "Func2", result: "Result 2: 56"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId4", "Func2", arguments: new Dictionary<string, object?> { { "i", 78 } }),
                new FunctionCallContent("callId5", "Func1")
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId4", "Func2", result: "Result 2: 78"),
                new FunctionResultContent("callId5", "Func1", result: "Result 1")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(s => new FunctionInvokingChatClient(s) { ConcurrentInvocation = concurrentInvocation });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
    }

    [Fact]
    public async Task ParallelFunctionCallsMayBeInvokedConcurrentlyAsync()
    {
        using var barrier = new Barrier(2);

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create((string arg) =>
                {
                    barrier.SignalAndWait();
                    return arg + arg;
                }, "Func"),
            ]
        };

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "Func", arguments: new Dictionary<string, object?> { { "arg", "hello" } }),
                new FunctionCallContent("callId2", "Func", arguments: new Dictionary<string, object?> { { "arg", "world" } }),
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", "Func", result: "hellohello"),
                new FunctionResultContent("callId2", "Func", result: "worldworld"),
            ]),
            new ChatMessage(ChatRole.Assistant, "done"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(s => new FunctionInvokingChatClient(s) { ConcurrentInvocation = true });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
    }

    [Fact]
    public async Task ConcurrentInvocationOfParallelCallsDisabledByDefaultAsync()
    {
        int activeCount = 0;

        var options = new ChatOptions
        {
            Tools =
            [
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

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "Func", arguments: new Dictionary<string, object?> { { "arg", "hello" } }),
                new FunctionCallContent("callId2", "Func", arguments: new Dictionary<string, object?> { { "arg", "world" } }),
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", "Func", result: "hellohello"),
                new FunctionResultContent("callId2", "Func", result: "worldworld"),
            ]),
            new ChatMessage(ChatRole.Assistant, "done"),
        ];

        await InvokeAndAssertAsync(options, plan);

        await InvokeAndAssertStreamingAsync(options, plan);
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

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage>? expected = keepFunctionCallingMessages ? null :
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, "world")
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = keepFunctionCallingMessages });

        Validate(await InvokeAndAssertAsync(options, plan, expected, configure));
        Validate(await InvokeAndAssertStreamingAsync(options, plan, expected, configure));

        void Validate(List<ChatMessage> finalChat)
        {
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

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new TextContent("extra"), new FunctionCallContent("callId1", "Func1"), new TextContent("stuff")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } }), new TextContent("more")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = keepFunctionCallingMessages });

#pragma warning disable SA1005, S125
        Validate(await InvokeAndAssertAsync(options, plan, keepFunctionCallingMessages ? null :
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new TextContent("extra"), new TextContent("stuff")]),
            new ChatMessage(ChatRole.Assistant, "more"),
            new ChatMessage(ChatRole.Assistant, "world"),
        ], configure));

        Validate(await InvokeAndAssertStreamingAsync(options, plan, keepFunctionCallingMessages ?
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", "Func2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", "VoidReturn", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "extrastuffmoreworld"),
        ] :
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, "extrastuffmoreworld"),
        ], configure));

        void Validate(List<ChatMessage> finalChat)
        {
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

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: detailedErrors ? "Error: Function failed. Exception: Oh no!" : "Error: Function failed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(s => new FunctionInvokingChatClient(s) { DetailedErrors = detailedErrors });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
    }

    [Fact]
    public async Task RejectsMultipleChoicesAsync()
    {
        var func1 = AIFunctionFactory.Create(() => "Some result 1", "Func1");
        var func2 = AIFunctionFactory.Create(() => "Some result 2", "Func2");

        var expected = new ChatCompletion(
        [
            new(ChatRole.Assistant, [new FunctionCallContent("callId1", func1.Metadata.Name)]),
            new(ChatRole.Assistant, [new FunctionCallContent("callId2", func2.Metadata.Name)]),
        ]);

        using var innerClient = new TestChatClient
        {
            CompleteAsyncCallback = async (chatContents, options, cancellationToken) =>
            {
                await Task.Yield();
                return expected;
            },
            CompleteStreamingAsyncCallback = (chatContents, options, cancellationToken) =>
              YieldAsync(expected.ToStreamingChatCompletionUpdates()),
        };

        IChatClient service = innerClient.AsBuilder().UseFunctionInvocation().Build();

        List<ChatMessage> chat = [new ChatMessage(ChatRole.User, "hello")];
        ChatOptions options = new() { Tools = [func1, func2] };

        Validate(await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(chat, options)));
        Validate(await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteStreamingAsync(chat, options).ToChatCompletionAsync()));

        void Validate(Exception ex)
        {
            Assert.Contains("only accepts a single choice", ex.Message);
            Assert.Single(chat); // It didn't add anything to the chat history
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task FunctionInvocationsLogged(LogLevel level)
    {
        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["arg1"] = "value1" })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "Result 1", "Func1")]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configure = b =>
            b.Use((c, services) => new FunctionInvokingChatClient(c, services.GetRequiredService<ILogger<FunctionInvokingChatClient>>()));

        await InvokeAsync(services => InvokeAndAssertAsync(options, plan, configurePipeline: configure, services: services));

        await InvokeAsync(services => InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure, services: services));

        async Task InvokeAsync(Func<IServiceProvider, Task> work)
        {
            var collector = new FakeLogCollector();

            ServiceCollection c = new();
            c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

            await work(c.BuildServiceProvider());

            var logs = collector.GetSnapshot();
            if (level is LogLevel.Trace)
            {
                Assert.Collection(logs,
                    entry => Assert.True(entry.Message.Contains("Invoking Func1({") && entry.Message.Contains("\"arg1\": \"value1\"")),
                    entry => Assert.True(entry.Message.Contains("Func1 invocation completed. Duration:") && entry.Message.Contains("Result: \"Result 1\"")));
            }
            else if (level is LogLevel.Debug)
            {
                Assert.Collection(logs,
                    entry => Assert.True(entry.Message.Contains("Invoking Func1") && !entry.Message.Contains("arg1")),
                    entry => Assert.True(entry.Message.Contains("Func1 invocation completed. Duration:") && !entry.Message.Contains("Result")));
            }
            else
            {
                Assert.Empty(logs);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FunctionInvocationTrackedWithActivity(bool enableTelemetry)
    {
        string sourceName = Guid.NewGuid().ToString();

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["arg1"] = "value1" })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", "Func1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        ChatOptions options = new()
        {
            Tools = [AIFunctionFactory.Create(() => "Result 1", "Func1")]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(c =>
            new FunctionInvokingChatClient(
                new OpenTelemetryChatClient(c, sourceName: sourceName)));

        await InvokeAsync(() => InvokeAndAssertAsync(options, plan, configurePipeline: configure));

        await InvokeAsync(() => InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure));

        async Task InvokeAsync(Func<Task> work)
        {
            var activities = new List<Activity>();
            using TracerProvider? tracerProvider = enableTelemetry ?
                OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                .AddSource(sourceName)
                .AddInMemoryExporter(activities)
                .Build() :
                null;

            await work();

            if (enableTelemetry)
            {
                Assert.Collection(activities,
                    activity => Assert.Equal("chat", activity.DisplayName),
                    activity => Assert.Equal("Func1", activity.DisplayName),
                    activity => Assert.Equal("chat", activity.DisplayName));
            }
            else
            {
                Assert.Empty(activities);
            }
        }
    }

    private static async Task<List<ChatMessage>> InvokeAndAssertAsync(
        ChatOptions options,
        List<ChatMessage> plan,
        List<ChatMessage>? expected = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        IServiceProvider? services = null)
    {
        Assert.NotEmpty(plan);

        configurePipeline ??= static b => b.UseFunctionInvocation();

        using CancellationTokenSource cts = new();
        List<ChatMessage> chat = [plan[0]];
        var expectedTotalTokenCounts = 0;

        using var innerClient = new TestChatClient
        {
            CompleteAsyncCallback = async (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Same(chat, contents);
                Assert.Equal(cts.Token, actualCancellationToken);

                await Task.Yield();

                var usage = CreateRandomUsage();
                expectedTotalTokenCounts += usage.InputTokenCount!.Value;
                return new ChatCompletion(new ChatMessage(ChatRole.Assistant, [.. plan[contents.Count].Contents])) { Usage = usage };
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build(services);

        var result = await service.CompleteAsync(chat, options, cts.Token);
        chat.Add(result.Message);

        expected ??= plan;
        Assert.NotNull(result);
        Assert.Equal(expected.Count, chat.Count);
        for (int i = 0; i < expected.Count; i++)
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

        // Usage should be aggregated over all responses, including AdditionalUsage
        var actualUsage = result.Usage!;
        Assert.Equal(expectedTotalTokenCounts, actualUsage.InputTokenCount);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.OutputTokenCount);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.TotalTokenCount);
        Assert.Equal(2, actualUsage.AdditionalCounts.Count);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.AdditionalCounts["firstValue"]);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.AdditionalCounts["secondValue"]);

        return chat;
    }

    private static UsageDetails CreateRandomUsage()
    {
        // We'll set the same random number on all the properties so that, when determining the
        // correct sum in tests, we only have to total the values once
        var value = new Random().Next(100);
        return new UsageDetails
        {
            InputTokenCount = value,
            OutputTokenCount = value,
            TotalTokenCount = value,
            AdditionalCounts = { ["firstValue"] = value, ["secondValue"] = value },
        };
    }

    private static async Task<List<ChatMessage>> InvokeAndAssertStreamingAsync(
        ChatOptions options,
        List<ChatMessage> plan,
        List<ChatMessage>? expected = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        IServiceProvider? services = null)
    {
        Assert.NotEmpty(plan);

        configurePipeline ??= static b => b.UseFunctionInvocation();

        using CancellationTokenSource cts = new();
        List<ChatMessage> chat = [plan[0]];

        using var innerClient = new TestChatClient
        {
            CompleteStreamingAsyncCallback = (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Same(chat, contents);
                Assert.Equal(cts.Token, actualCancellationToken);

                return YieldAsync(new ChatCompletion(new ChatMessage(ChatRole.Assistant, [.. plan[contents.Count].Contents])).ToStreamingChatCompletionUpdates());
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build(services);

        var result = await service.CompleteStreamingAsync(chat, options, cts.Token).ToChatCompletionAsync();
        chat.Add(result.Message);

        expected ??= plan;
        Assert.NotNull(result);
        Assert.Equal(expected.Count, chat.Count);
        for (int i = 0; i < expected.Count; i++)
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

    private static async IAsyncEnumerable<T> YieldAsync<T>(params T[] items)
    {
        await Task.Yield();
        foreach (var item in items)
        {
            yield return item;
        }
    }
}
