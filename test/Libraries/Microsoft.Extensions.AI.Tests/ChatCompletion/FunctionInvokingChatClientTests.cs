// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
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

        Assert.False(client.AllowConcurrentInvocation);
        Assert.False(client.IncludeDetailedErrors);
        Assert.Equal(10, client.MaximumIterationsPerRequest);
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
                AIFunctionFactory.Create((int? i = 42) => "Result 1", "Func1"),
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
            s => new FunctionInvokingChatClient(s) { AllowConcurrentInvocation = concurrentInvocation });

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
            s => new FunctionInvokingChatClient(s) { AllowConcurrentInvocation = true });

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

        await InvokeAndAssertAsync(options, plan);

        await InvokeAndAssertStreamingAsync(options, plan);
    }

    [Fact]
    public async Task ContinuesWithSuccessfulCallsUntilMaximumIterations()
    {
        var maxIterations = 7;
        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = pipeline => pipeline
            .UseFunctionInvocation(configure: functionInvokingChatClient =>
            {
                functionInvokingChatClient.MaximumIterationsPerRequest = maxIterations;
            });

        var actualCallCount = 0;
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => { actualCallCount++; }, "VoidReturn"),
            ]
        };

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"callId0", "VoidReturn")]),
        ];

        // Note that this plan ends with a function call. Normally we would expect the system to try to resolve
        // the call, but it won't because of the maximum iterations limit.
        for (var i = 0; i < maxIterations; i++)
        {
            plan.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent($"callId{i}", result: "Success: Function completed.")]));
            plan.Add(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent($"callId{(i + 1)}", "VoidReturn")]));
        }

        await InvokeAndAssertAsync(options, plan, configurePipeline: configurePipeline);
        Assert.Equal(maxIterations, actualCallCount);

        actualCallCount = 0;
        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configurePipeline);
        Assert.Equal(maxIterations, actualCallCount);
    }

    [Fact]
    public async Task KeepsFunctionCallingContent()
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

#pragma warning disable SA1005, S125
        Validate(await InvokeAndAssertAsync(options, plan));

        Validate(await InvokeAndAssertStreamingAsync(options, plan));

        static void Validate(List<ChatMessage> finalChat)
        {
            IEnumerable<AIContent> content = finalChat.SelectMany(m => m.Contents);
            Assert.Contains(content, c => c is FunctionCallContent or FunctionResultContent);
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
            s => new FunctionInvokingChatClient(s) { IncludeDetailedErrors = detailedErrors });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
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
            b.Use((c, services) => new FunctionInvokingChatClient(c, services.GetRequiredService<ILoggerFactory>()));

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
            new FunctionInvokingChatClient(new OpenTelemetryChatClient(c, sourceName: sourceName)));

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
                // Otherwise just end the conversation.
                List<ChatResponseUpdate> updates;
                string messageId = Guid.NewGuid().ToString("N");
                if (chatContents.Last().Text == "Hello")
                {
                    updates =
                    [
                        new() { Contents = [new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["text"] = "Input 1" })] },
                        new() { Contents = [new FunctionCallContent("callId2", "Func1", new Dictionary<string, object?> { ["text"] = "Input 2" })] }
                    ];
                }
                else
                {
                    updates = [new() { Contents = [new TextContent("OK bye")] }];
                }

                foreach (var update in updates)
                {
                    update.MessageId = messageId;
                }

                return YieldAsync(updates);
            }
        };

        using var client = new FunctionInvokingChatClient(innerClient);

        var response = await client.GetStreamingResponseAsync(messages, options, CancellationToken.None).ToChatResponseAsync();

        // The returned message should include the FCCs and FRCs.
        Assert.Collection(response.Messages,
            m => Assert.Collection(m.Contents,
                c => Assert.Equal("Input 1", Assert.IsType<FunctionCallContent>(c).Arguments!["text"]),
                c => Assert.Equal("Input 2", Assert.IsType<FunctionCallContent>(c).Arguments!["text"])),
            m => Assert.Collection(m.Contents,
                c => Assert.Equal("Result for Input 1", Assert.IsType<FunctionResultContent>(c).Result?.ToString()),
                c => Assert.Equal("Result for Input 2", Assert.IsType<FunctionResultContent>(c).Result?.ToString())),
            m => Assert.Equal("OK bye", Assert.IsType<TextContent>(Assert.Single(m.Contents)).Text));
    }

    [Fact]
    public async Task AllResponseMessagesReturned()
    {
        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(() => "doesn't matter", "Func1")]
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello"),
        };

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = async (chatContents, chatOptions, cancellationToken) =>
            {
                await Task.Yield();

                ChatMessage message = chatContents.Count() is 1 or 3 ?
                    new(ChatRole.Assistant, [new FunctionCallContent($"callId{chatContents.Count()}", "Func1")]) :
                    new(ChatRole.Assistant, "The answer is 42.");

                return new(message);
            }
        };

        using var client = new FunctionInvokingChatClient(innerClient);

        ChatResponse response = await client.GetResponseAsync(messages, options);

        Assert.Equal(5, response.Messages.Count);
        Assert.Equal("The answer is 42.", response.Text);
        Assert.IsType<FunctionCallContent>(Assert.Single(response.Messages[0].Contents));
        Assert.IsType<FunctionResultContent>(Assert.Single(response.Messages[1].Contents));
        Assert.IsType<FunctionCallContent>(Assert.Single(response.Messages[2].Contents));
        Assert.IsType<FunctionResultContent>(Assert.Single(response.Messages[3].Contents));
        Assert.IsType<TextContent>(Assert.Single(response.Messages[4].Contents));
    }

    [Fact]
    public async Task CanAccesssFunctionInvocationContextFromFunctionCall()
    {
        var invocationContexts = new List<FunctionInvocationContext>();
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

        await InvokeAsync(() => InvokeAndAssertAsync(options, plan, planBeforeTermination));

        await InvokeAsync(() => InvokeAndAssertStreamingAsync(options, plan, planBeforeTermination));

        // The current context should be null outside the async call stack for the function invocation
        Assert.Null(FunctionInvokingChatClient.CurrentContext);

        async Task InvokeAsync(Func<Task<List<ChatMessage>>> work)
        {
            invocationContexts.Clear();

            var messages = await work();

            Assert.Collection(invocationContexts,
                c => AssertInvocationContext(c, iteration: 0, terminate: false),
                c => AssertInvocationContext(c, iteration: 1, terminate: true));

            void AssertInvocationContext(FunctionInvocationContext context, int iteration, bool terminate)
            {
                Assert.NotNull(context);
                Assert.Equal(messages.Count, context.Messages.Count);
                Assert.Equal(string.Concat(messages), string.Concat(context.Messages));
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

        Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, ChatResponse> callback =
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

    [Fact]
    public async Task FunctionInvocations_PassesServices()
    {
        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["arg1"] = "value1" })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        ServiceCollection c = new();
        IServiceProvider expected = c.BuildServiceProvider();

        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((IServiceProvider actual) =>
            {
                Assert.Same(expected, actual);
                return "Result 1";
            }, "Func1")]
        };

        await InvokeAndAssertAsync(options, plan, services: expected);
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

                var message = new ChatMessage(ChatRole.Assistant, [.. plan[contents.Count()].Contents])
                {
                    MessageId = Guid.NewGuid().ToString("N")
                };
                return new ChatResponse(message) { Usage = usage };
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build(services);

        var result = await service.GetResponseAsync(new EnumeratedOnceEnumerable<ChatMessage>(chat), options, cts.Token);
        Assert.NotNull(result);

        chat.AddRange(result.Messages);

        expected ??= plan;
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

                ChatMessage message = new(ChatRole.Assistant, [.. plan[contents.Count()].Contents])
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                };
                return YieldAsync(new ChatResponse(message).ToChatResponseUpdates());
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build(services);

        var result = await service.GetStreamingResponseAsync(new EnumeratedOnceEnumerable<ChatMessage>(chat), options, cts.Token).ToChatResponseAsync();
        Assert.NotNull(result);

        chat.AddRange(result.Messages);

        expected ??= plan;
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

    private static async IAsyncEnumerable<T> YieldAsync<T>(params IEnumerable<T> items)
    {
        await Task.Yield();
        foreach (var item in items)
        {
            yield return item;
        }
    }

    private sealed class EnumeratedOnceEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        private int _iterated;

        public IEnumerator<T> GetEnumerator()
        {
            if (Interlocked.Exchange(ref _iterated, 1) != 0)
            {
                throw new InvalidOperationException("This enumerable can only be enumerated once.");
            }

            foreach (var item in items)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
