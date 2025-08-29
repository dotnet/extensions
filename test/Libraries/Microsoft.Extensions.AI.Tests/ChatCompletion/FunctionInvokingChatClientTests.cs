// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
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
        Assert.Equal(40, client.MaximumIterationsPerRequest);
        Assert.Equal(3, client.MaximumConsecutiveErrorsPerRequest);
        Assert.Null(client.FunctionInvoker);
        Assert.Null(client.AdditionalTools);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        using TestChatClient innerClient = new();
        using FunctionInvokingChatClient client = new(innerClient);

        Assert.False(client.AllowConcurrentInvocation);
        client.AllowConcurrentInvocation = true;
        Assert.True(client.AllowConcurrentInvocation);

        Assert.False(client.IncludeDetailedErrors);
        client.IncludeDetailedErrors = true;
        Assert.True(client.IncludeDetailedErrors);

        Assert.Equal(40, client.MaximumIterationsPerRequest);
        client.MaximumIterationsPerRequest = 5;
        Assert.Equal(5, client.MaximumIterationsPerRequest);

        Assert.Equal(3, client.MaximumConsecutiveErrorsPerRequest);
        client.MaximumConsecutiveErrorsPerRequest = 1;
        Assert.Equal(1, client.MaximumConsecutiveErrorsPerRequest);

        Assert.Null(client.FunctionInvoker);
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> invoker = (ctx, ct) => new ValueTask<object?>("test");
        client.FunctionInvoker = invoker;
        Assert.Same(invoker, client.FunctionInvoker);

        Assert.Null(client.AdditionalTools);
        IList<AITool> additionalTools = [AIFunctionFactory.Create(() => "Additional Tool")];
        client.AdditionalTools = additionalTools;
        Assert.Same(additionalTools, client.AdditionalTools);
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
    public async Task SupportsToolsProvidedByAdditionalTools(bool provideOptions)
    {
        ChatOptions? options = provideOptions ?
            new() { Tools = [AIFunctionFactory.Create(() => "Shouldn't be invoked", "ChatOptionsFunc")] } :
            null;

        Func<ChatClientBuilder, ChatClientBuilder> configure = builder =>
            builder.UseFunctionInvocation(configure: c => c.AdditionalTools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((int i) => { }, "VoidReturn"),
            ]);

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

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
    }

    [Fact]
    public async Task PrefersToolsProvidedByChatOptions()
    {
        ChatOptions options = new()
        {
            Tools = [AIFunctionFactory.Create(() => "Result 1", "Func1")]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configure = builder =>
            builder.UseFunctionInvocation(configure: c => c.AdditionalTools =
            [
                AIFunctionFactory.Create(() => "Should never be invoked", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((int i) => { }, "VoidReturn"),
            ]);

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

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
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
        int remaining = 2;
        var tcs = new TaskCompletionSource<bool>();

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(async (string arg) =>
                {
                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        tcs.SetResult(true);
                    }

                    await tcs.Task;

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
    public async Task FunctionInvokerDelegateOverridesHandlingAsync()
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
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1 from delegate")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42 from delegate")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "VoidReturn", arguments: new Dictionary<string, object?> { { "i", 43 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Success: Function completed.")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        Func<ChatClientBuilder, ChatClientBuilder> configure = b => b.Use(
            s => new FunctionInvokingChatClient(s)
            {
                FunctionInvoker = async (ctx, cancellationToken) =>
                {
                    Assert.NotNull(ctx);
                    var result = await ctx.Function.InvokeAsync(ctx.Arguments, cancellationToken);
                    return result is JsonElement e ?
                        JsonSerializer.SerializeToElement($"{e.GetString()} from delegate", AIJsonUtilities.DefaultOptions) :
                        result;
                }
            });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configure);

        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContinuesWithFailingCallsUntilMaximumConsecutiveErrors(bool allowConcurrentInvocation)
    {
        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = pipeline => pipeline
            .UseFunctionInvocation(configure: functionInvokingChatClient =>
            {
                functionInvokingChatClient.MaximumConsecutiveErrorsPerRequest = 2;
                functionInvokingChatClient.AllowConcurrentInvocation = allowConcurrentInvocation;
            });

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create((bool shouldThrow, int callIndex) =>
                {
                    if (shouldThrow)
                    {
                        throw new InvalidTimeZoneException($"Exception from call {callIndex}");
                    }
                }, "Func"),
            ]
        };

        var callIndex = 0;
        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),

            // A single failure isn't enough to stop the cycle
            ..CreateFunctionCallIterationPlan(ref callIndex, true, false),

            // Now NumConsecutiveErrors = 1
            // We can reset the number of consecutive errors by having a successful iteration
            ..CreateFunctionCallIterationPlan(ref callIndex, false, false, false),

            // Now NumConsecutiveErrors = 0
            // Any failure within an iteration causes the whole iteration to be treated as failed
            ..CreateFunctionCallIterationPlan(ref callIndex, false, true, false),

            // Now NumConsecutiveErrors = 1
            // Even if several calls in the same iteration fail, that only counts as a single iteration having failed, so won't exceed the limit yet
            ..CreateFunctionCallIterationPlan(ref callIndex, true, true, true),

            // Now NumConsecutiveErrors = 2
            // Any more failures will now exceed the limit
            ..CreateFunctionCallIterationPlan(ref callIndex, true, true),
        ];

        if (allowConcurrentInvocation)
        {
            // With concurrent invocation, we always make all the calls in the iteration
            // and combine their exceptions into an AggregateException
            var ex = await Assert.ThrowsAsync<AggregateException>(() =>
                InvokeAndAssertAsync(options, plan, configurePipeline: configurePipeline));
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.Equal("Exception from call 11", ex.InnerExceptions[0].Message);
            Assert.Equal("Exception from call 12", ex.InnerExceptions[1].Message);

            ex = await Assert.ThrowsAsync<AggregateException>(() =>
                InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configurePipeline));
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.Equal("Exception from call 11", ex.InnerExceptions[0].Message);
            Assert.Equal("Exception from call 12", ex.InnerExceptions[1].Message);
        }
        else
        {
            // With serial invocation, we allow the threshold-crossing exception to propagate
            // directly and terminate the iteration
            var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
                InvokeAndAssertAsync(options, plan, configurePipeline: configurePipeline));
            Assert.Equal("Exception from call 11", ex.Message);

            ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
                InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configurePipeline));
            Assert.Equal("Exception from call 11", ex.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanFailOnFirstException(bool allowConcurrentInvocation)
    {
        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = pipeline => pipeline
            .UseFunctionInvocation(configure: functionInvokingChatClient =>
            {
                functionInvokingChatClient.MaximumConsecutiveErrorsPerRequest = 0;
                functionInvokingChatClient.AllowConcurrentInvocation = allowConcurrentInvocation;
            });

        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() =>
                {
                    throw new InvalidTimeZoneException($"It failed");
                }, "Func"),
            ]
        };

        var callIndex = 0;
        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            ..CreateFunctionCallIterationPlan(ref callIndex, true),
        ];

        // Regardless of AllowConcurrentInvocation, if there's only a single exception,
        // we don't wrap it in an AggregateException
        var ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
            InvokeAndAssertAsync(options, plan, configurePipeline: configurePipeline));
        Assert.Equal("It failed", ex.Message);

        ex = await Assert.ThrowsAsync<InvalidTimeZoneException>(() =>
            InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configurePipeline));
        Assert.Equal("It failed", ex.Message);
    }

    private static IEnumerable<ChatMessage> CreateFunctionCallIterationPlan(ref int callIndex, params bool[] shouldThrow)
    {
        var assistantMessage = new ChatMessage(ChatRole.Assistant, []);
        var toolMessage = new ChatMessage(ChatRole.Tool, []);

        foreach (var callShouldThrow in shouldThrow)
        {
            var thisCallIndex = callIndex++;
            var callId = $"callId{thisCallIndex}";
            assistantMessage.Contents.Add(new FunctionCallContent(callId, "Func",
                arguments: new Dictionary<string, object?> { { "shouldThrow", callShouldThrow }, { "callIndex", thisCallIndex } }));
            toolMessage.Contents.Add(new FunctionResultContent(callId, result: callShouldThrow ? "Error: Function failed." : "Success"));
        }

        return [assistantMessage, toolMessage];
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

        await InvokeAsync(() => InvokeAndAssertAsync(options, plan, configurePipeline: configure), streaming: false);

        await InvokeAsync(() => InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configure), streaming: true);

        async Task InvokeAsync(Func<Task> work, bool streaming)
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
                    activity => Assert.Equal("execute_tool Func1", activity.DisplayName),
                    activity => Assert.Equal("chat", activity.DisplayName),
                    activity => Assert.Equal(streaming ? "FunctionInvokingChatClient.GetStreamingResponseAsync" : "FunctionInvokingChatClient.GetResponseAsync", activity.DisplayName));

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
    public async Task HaltFunctionCallingAfterTermination()
    {
        var function = AIFunctionFactory.Create((string? result = null) =>
        {
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            FunctionInvokingChatClient.CurrentContext!.Terminate = true;
            return (object?)null;
        }, "Search");

        using var innerChatClient = new TestChatClient
        {
            GetResponseAsyncCallback = (chatContents, chatOptions, cancellationToken) =>
            {
                // We can have a mixture of calls that are not terminated and terminated
                var existingSearchResult = chatContents.SingleOrDefault(m => m.Role == ChatRole.Tool);
                AIContent[] resultContents = existingSearchResult is not null && existingSearchResult.Contents.OfType<FunctionResultContent>().ToList() is { } frcs
                    ? [new TextContent($"The search results were '{string.Join(", ", frcs.Select(frc => frc.Result))}'")]
                    : [
                        new FunctionCallContent("callId1", "Search"),
                        new FunctionCallContent("callId2", "Search", new Dictionary<string, object?> { { "result", "birds" } }),
                        new FunctionCallContent("callId3", "Search"),
                      ];

                var message = new ChatMessage(ChatRole.Assistant, resultContents);
                return Task.FromResult(new ChatResponse(message));
            }
        };
        using var chatClient = new FunctionInvokingChatClient(innerChatClient);

        // The function should terminate the invocation loop without calling the inner client for a final answer
        // But it still makes all the function calls within the same iteration
        List<ChatMessage> messages = [new(ChatRole.User, "hello")];
        var chatOptions = new ChatOptions { Tools = [function] };
        var result = await chatClient.GetResponseAsync(messages, chatOptions);
        messages.AddMessages(result);

        // Application code can then set the results
        var lastMessage = messages.Last();
        Assert.Equal(ChatRole.Tool, lastMessage.Role);
        var frcs = lastMessage.Contents.OfType<FunctionResultContent>().ToList();
        Assert.Single(frcs);
        frcs[0].Result = "dogs";

        // We can re-enter the function calling mechanism to get a final answer
        result = await chatClient.GetResponseAsync(messages, chatOptions);
        Assert.Equal("The search results were 'dogs'", result.Text);
    }

    [Fact]
    public async Task PropagatesResponseConversationIdToOptions()
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
                    Assert.Null(chatOptions?.ConversationId);
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId-abc", "Func1")]))
                    {
                        ConversationId = "12345",
                    };
                }
                else if (iteration == 2)
                {
                    Assert.Equal("12345", chatOptions?.ConversationId);
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

    [Fact]
    public async Task FunctionInvocations_InvokedOnOriginalSynchronizationContext()
    {
        SynchronizationContext ctx = new CustomSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(ctx);

        List<ChatMessage> plan =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [
                new FunctionCallContent("callId1", "Func1", new Dictionary<string, object?> { ["arg"] = "value1" }),
                new FunctionCallContent("callId2", "Func1", new Dictionary<string, object?> { ["arg"] = "value2" }),
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId2", result: "value1"),
                new FunctionResultContent("callId2", result: "value2")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        var options = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(async (string arg, CancellationToken cancellationToken) =>
            {
                await Task.Delay(1, cancellationToken);
                Assert.Same(ctx, SynchronizationContext.Current);
                return arg;
            }, "Func1")]
        };

        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = builder => builder
            .Use(async (messages, options, next, cancellationToken) =>
            {
                await Task.Delay(1, cancellationToken);
                await next(messages, options, cancellationToken);
            })
            .UseOpenTelemetry()
            .UseFunctionInvocation(configure: c => { c.AllowConcurrentInvocation = true; c.IncludeDetailedErrors = true; });

        await InvokeAndAssertAsync(options, plan, configurePipeline: configurePipeline);
        await InvokeAndAssertStreamingAsync(options, plan, configurePipeline: configurePipeline);
    }

    private sealed class CustomSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SetSynchronizationContext(this);
                d(state);
            });
        }
    }

    private static async Task<List<ChatMessage>> InvokeAndAssertAsync(
        ChatOptions? options,
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
        AssertExtensions.EqualMessageLists(expected, chat);

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
        ChatOptions? options,
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

        AssertExtensions.EqualMessageLists(expected, chat);
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
}
