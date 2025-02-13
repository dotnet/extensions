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
    private readonly Func<ChatClientBuilder, ChatClientBuilder> _keepMessagesConfigure =
        b => b.Use(client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = true });

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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, plan, configurePipeline: _keepMessagesConfigure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: _keepMessagesConfigure);
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
                new FunctionResultContent("callId1", result: "Result 1"),
                new FunctionResultContent("callId2", result: "Result 2: 34"),
                new FunctionResultContent("callId3", result: "Result 2: 56"),
            ]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId4", "Func2", arguments: new Dictionary<string, object?> { { "i", 78 } }),
                new FunctionCallContent("callId5", "Func1")
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId4", result: "Result 2: 78"),
                new FunctionResultContent("callId5", result: "Result 1")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(
            s => new FunctionInvokingChatClient(s) { ConcurrentInvocation = concurrentInvocation, KeepFunctionCallingMessages = true });

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
                new FunctionResultContent("callId1", result: "hellohello"),
                new FunctionResultContent("callId2", result: "worldworld"),
            ]),
            new ChatMessage(ChatRole.Assistant, "done"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(
            s => new FunctionInvokingChatClient(s) { ConcurrentInvocation = true, KeepFunctionCallingMessages = true });

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
                new FunctionResultContent("callId1", result: "hellohello"),
                new FunctionResultContent("callId2", result: "worldworld"),
            ]),
            new ChatMessage(ChatRole.Assistant, "done"),
        ];

        await InvokeAndAssertAsync(options, plan, configurePipeline: _keepMessagesConfigure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: _keepMessagesConfigure);
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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage>? expected = keepFunctionCallingMessages ? null :
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, "world")
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(
            client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = keepFunctionCallingMessages });

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
    public async Task KeepsFunctionCallingContentWhenRequestedAsync(bool keepFunctionCallingMessages)
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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } }), new TextContent("more")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(
            client => new FunctionInvokingChatClient(client) { KeepFunctionCallingMessages = keepFunctionCallingMessages });

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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Success: Function completed.")]),
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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: detailedErrors ? "Error: Function failed. Exception: Oh no!" : "Error: Function failed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(
            s => new FunctionInvokingChatClient(s) { DetailedErrors = detailedErrors, KeepFunctionCallingMessages = true });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
    }

    [Fact]
    public async Task RejectsMultipleChoicesAsync()
    {
        var func1 = AIFunctionFactory.Create(() => "Some result 1", "Func1");
        var func2 = AIFunctionFactory.Create(() => "Some result 2", "Func2");

        var expected = new ChatResponse(
        [
            new(ChatRole.Assistant, [new FunctionCallContent("callId1", func1.Name)]),
            new(ChatRole.Assistant, [new FunctionCallContent("callId2", func2.Name)]),
        ]);

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = async (chatContents, options, cancellationToken) =>
            {
                await Task.Yield();
                return expected;
            },
            GetStreamingResponseAsyncCallback = (chatContents, options, cancellationToken) =>
              YieldAsync(expected.ToChatResponseUpdates()),
        };

        IChatClient service = innerClient.AsBuilder().UseFunctionInvocation().Build();

        List<ChatMessage> chat = [new ChatMessage(ChatRole.User, "hello")];
        ChatOptions options = new() { Tools = [func1, func2] };

        Validate(await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetResponseAsync(chat, options)));
        Validate(await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetStreamingResponseAsync(chat, options).ToChatResponseAsync()));

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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "Result 1", "Func1")]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configure = b =>
            b.Use((c, services) => new FunctionInvokingChatClient(c, services.GetRequiredService<ILogger<FunctionInvokingChatClient>>())
            {
                KeepFunctionCallingMessages = true,
            });

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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        ChatOptions options = new()
        {
            Tools = [AIFunctionFactory.Create(() => "Result 1", "Func1")]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(c =>
            new FunctionInvokingChatClient(new OpenTelemetryChatClient(c, sourceName: sourceName))
            {
                KeepFunctionCallingMessages = true,
            });

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
                    activity => Assert.Equal("chat", activity.DisplayName),
                    activity => Assert.Equal(nameof(FunctionInvokingChatClient), activity.DisplayName));

                for (int i = 0; i < activities.Count - 1; i++)
                {
                    // Activities are exported in the order of completion, so all except the last are children of the last (i.e., outer)
                    Assert.Same(activities[activities.Count - 1], activities[i].Parent);
                }
            }
            else
            {
                Assert.Empty(activities);
            }
        }
    }

    [Fact]
    public async Task SupportsConsecutiveStreamingUpdatesWithFunctionCalls()
    {
        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((string text) => $"Result for {text}", "Func1")]
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
        };

        using var innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (chatContents, chatOptions, cancellationToken) =>
            {
                // If the conversation is just starting, issue two consecutive updates with function calls
                // Otherwise just end the conversation
                return chatContents.Last().Text == "Hello"
                    ? YieldAsync(
                        new ChatResponseUpdate { Contents = [new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["text"] = "Input 1" })] },
                        new ChatResponseUpdate { Contents = [new FunctionCallContent("callId2", "Func1", new Dictionary<string, object?> { ["text"] = "Input 2" })] })
                    : YieldAsync(
                        new ChatResponseUpdate { Contents = [new TextContent("OK bye")] });
            }
        };

        using var client = new FunctionInvokingChatClient(innerClient) { KeepFunctionCallingMessages = true };

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages, options, CancellationToken.None))
        {
            updates.Add(update);
        }

        // Message history should now include the FCCs and FRCs
        Assert.Collection(messages,
            m => Assert.Equal("Hello", Assert.IsType<TextContent>(Assert.Single(m.Contents)).Text),
            m => Assert.Collection(m.Contents,
                c => Assert.Equal("Input 1", Assert.IsType<FunctionCallContent>(c).Arguments!["text"]),
                c => Assert.Equal("Input 2", Assert.IsType<FunctionCallContent>(c).Arguments!["text"])),
            m => Assert.Collection(m.Contents,
                c => Assert.Equal("Result for Input 1", Assert.IsType<FunctionResultContent>(c).Result?.ToString()),
                c => Assert.Equal("Result for Input 2", Assert.IsType<FunctionResultContent>(c).Result?.ToString())));

        // The returned updates should *not* include the FCCs and FRCs
        var allUpdateContents = updates.SelectMany(updates => updates.Contents).ToList();
        var singleUpdateContent = Assert.IsType<TextContent>(Assert.Single(allUpdateContents));
        Assert.Equal("OK bye", singleUpdateContent.Text);
    }

    [Fact]
    public async Task CanAccesssFunctionInvocationContextFromFunctionCall()
    {
        var invocationContexts = new List<FunctionInvokingChatClient.FunctionInvocationContext>();
        var function = AIFunctionFactory.Create(async (int i) =>
        {
            // The context should propogate across async calls
            await Task.Yield();

            var context = FunctionInvokingChatClient.CurrentContext!;
            invocationContexts.Add(context);

            if (i == 42)
            {
                context.Terminate = true;
            }

            return $"Result {i}";
        }, "Func1");

        var options = new ChatOptions
        {
            Tools = [function],
        };

        // The invocation loop should terminate after the second function call
        List<ChatMessage> planBeforeTermination =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["i"] = 41 })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 41")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func1", new Dictionary<string, object?> { ["i"] = 42 })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 42")]),
        ];

        // The full plan should never be fulfilled
        List<ChatMessage> plan =
        [
            .. planBeforeTermination,
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "Func1", new Dictionary<string, object?> { ["i"] = 43 })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Result 43")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAsync(() => InvokeAndAssertAsync(options, plan, expected: [
            .. planBeforeTermination,

            // The last message is the one returned by the chat client
            // This message's content should contain the last function call before the termination
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func1", new Dictionary<string, object?> { ["i"] = 42 })]),
        ], configurePipeline: _keepMessagesConfigure));

        await InvokeAsync(() => InvokeAndAssertStreamingAsync(options, plan, expected: [
            .. planBeforeTermination,

            // The last message is the one returned by the chat client
            // When streaming, function call content is removed from this message
            new ChatMessage(ChatRole.Assistant, []),
        ], configurePipeline: _keepMessagesConfigure));

        // The current context should be null outside the async call stack for the function invocation
        Assert.Null(FunctionInvokingChatClient.CurrentContext);

        async Task InvokeAsync(Func<Task<List<ChatMessage>>> work)
        {
            invocationContexts.Clear();

            var chatMessages = await work();

            Assert.Collection(invocationContexts,
                c => AssertInvocationContext(c, iteration: 0, terminate: false),
                c => AssertInvocationContext(c, iteration: 1, terminate: true));

            void AssertInvocationContext(FunctionInvokingChatClient.FunctionInvocationContext context, int iteration, bool terminate)
            {
                Assert.NotNull(context);
                Assert.Same(chatMessages, context.ChatMessages);
                Assert.Same(function, context.Function);
                Assert.Equal("Func1", context.CallContent.Name);
                Assert.Equal(0, context.FunctionCallIndex);
                Assert.Equal(1, context.FunctionCount);
                Assert.Equal(iteration, context.Iteration);
                Assert.Equal(terminate, context.Terminate);
            }
        }
    }

    [Fact]
    public async Task PropagatesResponseChatThreadIdToOptions()
    {
        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "Result 1", "Func1")],
        };

        int iteration = 0;

        Func<IList<ChatMessage>, ChatOptions?, CancellationToken, ChatResponse> callback =
            (chatContents, chatOptions, cancellationToken) =>
            {
                iteration++;

                if (iteration == 1)
                {
                    Assert.Null(chatOptions?.ChatThreadId);
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId-abc", "Func1")]))
                    {
                        ChatThreadId = "12345",
                    };
                }
                else if (iteration == 2)
                {
                    Assert.Equal("12345", chatOptions?.ChatThreadId);
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, "done!"));
                }
                else
                {
                    throw new InvalidOperationException("Unexpected iteration");
                }
            };

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = (chatContents, chatOptions, cancellationToken) =>
                Task.FromResult(callback(chatContents, chatOptions, cancellationToken)),
            GetStreamingResponseAsyncCallback = (chatContents, chatOptions, cancellationToken) =>
                YieldAsync(callback(chatContents, chatOptions, cancellationToken).ToChatResponseUpdates()),
        };

        using IChatClient service = innerClient.AsBuilder().UseFunctionInvocation().Build();

        iteration = 0;
        Assert.Equal("done!", (await service.GetResponseAsync("hey", options)).ToString());
        iteration = 0;
        Assert.Equal("done!", (await service.GetStreamingResponseAsync("hey", options).ToChatResponseAsync()).ToString());
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
        long expectedTotalTokenCounts = 0;

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = async (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Equal(cts.Token, actualCancellationToken);

                await Task.Yield();

                var usage = CreateRandomUsage();
                expectedTotalTokenCounts += usage.InputTokenCount!.Value;
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, [.. plan[contents.Count].Contents])) { Usage = usage };
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build(services);

        var result = await service.GetResponseAsync(chat, options, cts.Token);
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
        Assert.Equal(2, actualUsage.AdditionalCounts!.Count);
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
            AdditionalCounts = new() { ["firstValue"] = value, ["secondValue"] = value },
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
            GetStreamingResponseAsyncCallback = (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Equal(cts.Token, actualCancellationToken);

                return YieldAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, [.. plan[contents.Count].Contents])).ToChatResponseUpdates());
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build(services);

        var result = await service.GetStreamingResponseAsync(chat, options, cts.Token).ToChatResponseAsync();
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
