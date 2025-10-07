// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ToolGroupingTests
{
    private const string ExpansionToolName = "__expand_tool_group";
    private static readonly Random _random = new();
    private static readonly object _randomLock = new();

    [Fact]
    public async Task ToolGroupingChatClient_Collapsed_IncludesExpansionAndUngroupedOnly()
    {
        var ungrouped = new SimpleTool("Basic", "basic");
        var groupedA = new SimpleTool("A1", "a1");
        var groupedB = new SimpleTool("B1", "b1");

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "hello")],
            Options = new ChatOptions { Tools = [ungrouped, AIToolGroup.Create("GroupA", "Group A", [groupedA]), AIToolGroup.Create("GroupB", "Group B", [groupedB])] },
            ConfigureToolGroupingOptions = options => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "Hi"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertResponse(ToolGroupingTestResult testResult) => Assert.Equal("Hi", testResult.Response.Text);

        AssertResponse(result);
        AssertResponse(streamingResult);

        void AssertObservedTools(List<IList<AITool>?> observedTools)
        {
            var tools = Assert.Single(observedTools);
            Assert.NotNull(tools);
            Assert.Contains(tools!, t => t.Name == ungrouped.Name);
            Assert.Contains(tools, t => t.Name == ExpansionToolName);
            Assert.DoesNotContain(tools, t => t.Name == groupedA.Name);
            Assert.DoesNotContain(tools, t => t.Name == groupedB.Name);
        }

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ExpansionLoop_ExpandsSingleGroup()
    {
        var groupedA1 = new SimpleTool("A1", "a1");
        var groupedA2 = new SimpleTool("A2", "a2");
        var groupedB = new SimpleTool("B1", "b1");
        var ungrouped = new SimpleTool("Common", "c");

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [ungrouped, AIToolGroup.Create("GroupA", "Group A", [groupedA1, groupedA2]), AIToolGroup.Create("GroupB", "Group B", [groupedB])] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 1;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "GroupA"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "Done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertResponse(ToolGroupingTestResult testResult) => Assert.Equal("Done", testResult.Response.Text);

        AssertResponse(result);
        AssertResponse(streamingResult);

        void AssertObservedTools(List<IList<AITool>?> observed) => Assert.Collection(observed,
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
                Assert.DoesNotContain(tools, t => t.Name == groupedA1.Name);
                Assert.DoesNotContain(tools, t => t.Name == groupedB.Name);
            },
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
                Assert.Contains(tools, t => t.Name == groupedA1.Name);
                Assert.Contains(tools, t => t.Name == groupedA2.Name);
                Assert.DoesNotContain(tools, t => t.Name == groupedB.Name);
            });

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);

        AssertContainsResultMessage(result.Response, "Successfully expanded group 'GroupA'");
        AssertContainsResultMessage(streamingResult.Response, "Successfully expanded group 'GroupA'");
    }

    [Fact]
    public async Task ToolGroupingChatClient_NoGroups_BypassesMiddleware()
    {
        var tool = new SimpleTool("Standalone", "s");

        ToolGroupingTestScenario CreateScenario(ChatOptions options, List<ChatOptions?> observedOptions, List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "hello")],
            Options = options,
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "ok"),
                    AssertInvocation = ctx =>
                    {
                        observedOptions.Add(ctx.Options);
                        observedTools.Add(ctx.Options?.Tools?.ToList());
                    }
                }
            ]
        };

        List<ChatOptions?> observedOptionsNonStreaming = [];
        List<IList<AITool>?> observedToolsNonStreaming = [];
        ChatOptions nonStreamingOptions = new() { Tools = [tool] };
        var result = await InvokeAndAssertAsync(CreateScenario(nonStreamingOptions, observedOptionsNonStreaming, observedToolsNonStreaming));

        List<ChatOptions?> observedOptionsStreaming = [];
        List<IList<AITool>?> observedToolsStreaming = [];
        ChatOptions streamingOptions = new() { Tools = [tool] };
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(streamingOptions, observedOptionsStreaming, observedToolsStreaming));

        void AssertResponse(ToolGroupingTestResult testResult) => Assert.Equal("ok", testResult.Response.Text);

        AssertResponse(result);
        AssertResponse(streamingResult);

        static void AssertObservedOptions(ChatOptions expected, List<ChatOptions?> observed) =>
            Assert.Same(expected, Assert.Single(observed));

        static void AssertObservedTools(List<IList<AITool>?> observed)
        {
            var tools = Assert.Single(observed);
            Assert.NotNull(tools);
            Assert.DoesNotContain(tools!, t => t.Name == ExpansionToolName);
        }

        AssertObservedOptions(nonStreamingOptions, observedOptionsNonStreaming);
        AssertObservedOptions(streamingOptions, observedOptionsStreaming);

        AssertObservedTools(observedToolsNonStreaming);
        AssertObservedTools(observedToolsStreaming);
    }

    [Fact]
    public async Task ToolGroupingChatClient_InvalidGroupRequest_ReturnsResultMessage()
    {
        var groupedA = new SimpleTool("A1", "a1");

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [AIToolGroup.Create("GroupA", "Group A", [groupedA])] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent(Guid.NewGuid().ToString("N"), ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = "Unknown" })])
                }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "was invalid; ignoring expansion request");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_MissingGroupName_ReturnsNotice()
    {
        var groupedA = new SimpleTool("A1", "a1");

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [AIToolGroup.Create("GroupA", "Group A", [groupedA])] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent(Guid.NewGuid().ToString("N"), ExpansionToolName)])
                }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "No group name was specified");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_GroupNameReadsJsonElement()
    {
        var groupedA = new SimpleTool("A1", "a1");
        var jsonValue = JsonDocument.Parse("\"GroupA\"").RootElement;

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [AIToolGroup.Create("GroupA", "Group A", [groupedA])] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent(Guid.NewGuid().ToString("N"), ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = jsonValue })])
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done")
                }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'GroupA'");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ExpansionLimitReached_AppendsLimitMessage()
    {
        var groupedA = new SimpleTool("A1", "a1");
        var groupedB = new SimpleTool("B1", "b1");
        var alwaysOn = new SimpleTool("Common", "c");

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [alwaysOn, AIToolGroup.Create("GroupA", "Group A", [groupedA]), AIToolGroup.Create("GroupB", "Group B", [groupedB])] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 1;
            },
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "GroupA") },
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call2", "GroupB") }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "Max expansion iteration count reached");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_MultipleValidExpansions_LastWins()
    {
        var groupedA = new SimpleTool("A1", "a1");
        var groupedB = new SimpleTool("B1", "b1");
        var alwaysOn = new SimpleTool("Common", "c");

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [alwaysOn, AIToolGroup.Create("GroupA", "Group A", [groupedA]), AIToolGroup.Create("GroupB", "Group B", [groupedB])] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new ChatMessage(ChatRole.Assistant,
                    [
                        new FunctionCallContent("call1", ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = "GroupA" }),
                        new FunctionCallContent("call2", ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = "GroupB" })
                    ]),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedToolsNonStreaming = [];
        var result = await InvokeAndAssertAsync(CreateScenario(observedToolsNonStreaming));

        List<IList<AITool>?> observedToolsStreaming = [];
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedToolsStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed) => Assert.Collection(observed,
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == alwaysOn.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
            },
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == alwaysOn.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
                Assert.DoesNotContain(tools, t => t.Name == groupedA.Name);
                Assert.Contains(tools, t => t.Name == groupedB.Name);
            });

        AssertObservedTools(observedToolsNonStreaming);
        AssertObservedTools(observedToolsStreaming);

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'GroupA'");
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'GroupB'");
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_DuplicateExpansionSameIteration_Reported()
    {
        var groupedA = new SimpleTool("A1", "a1");

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [AIToolGroup.Create("GroupA", "Group A", [groupedA])] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new ChatMessage(ChatRole.Assistant,
                    [
                        new FunctionCallContent("call1", ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = "GroupA" }),
                        new FunctionCallContent("call2", ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = "GroupA" })
                    ])
                },
                new DownstreamTurn { ResponseMessage = new(ChatRole.Assistant, "done") }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "Ignoring duplicate expansion");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ReexpandingSameGroupTerminatesLoop()
    {
        var groupedA = new SimpleTool("A1", "a1");

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [AIToolGroup.Create("GroupA", "Group A", [groupedA])] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "GroupA") },
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call2", "GroupA") }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "Ignoring duplicate expansion of group 'GroupA'.");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_PropagatesConversationIdBetweenIterations()
    {
        var groupedA = new SimpleTool("A1", "a1");

        ToolGroupingTestScenario CreateScenario(List<string?> observedConversationIds) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            Options = new ChatOptions { Tools = [AIToolGroup.Create("GroupA", "Group A", [groupedA])] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "GroupA"),
                    ConversationId = "conv-1"
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedConversationIds.Add(ctx.Options?.ConversationId)
                }
            ]
        };

        List<string?> observedNonStreaming = [];
        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));

        List<string?> observedStreaming = [];
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        static void AssertConversationIds(List<string?> observed) => Assert.Equal("conv-1", Assert.Single(observed));

        AssertConversationIds(observedNonStreaming);
        AssertConversationIds(observedStreaming);

        void AssertResponse(ToolGroupingTestResult testResult) => Assert.Equal("done", testResult.Response.Text);

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    private static async Task<ToolGroupingTestResult> InvokeAndAssertAsync(ToolGroupingTestScenario scenario)
    {
        if (scenario.InitialMessages.Count == 0)
        {
            throw new InvalidOperationException("Scenario must include at least one initial message.");
        }

        List<DownstreamTurn> turns = scenario.Turns;
        int iteration = 0;
        List<UsageDetails> generatedUsage = [];

        using TestChatClient inner = new();

        inner.GetResponseAsyncCallback = (messages, options, cancellationToken) =>
        {
            var materialized = messages.ToList();
            if (iteration >= turns.Count)
            {
                throw new InvalidOperationException("Unexpected additional iteration.");
            }

            var turn = turns[iteration];
            turn.AssertInvocation?.Invoke(new ToolGroupingInvocationContext(iteration, materialized, options));

            UsageDetails usage = CreateRandomUsage();
            generatedUsage.Add(usage);

            var response = CreateResponse(turn, usage);
            iteration++;
            return Task.FromResult(response);
        };

        using IChatClient client = inner.AsBuilder().UseToolGrouping(scenario.ConfigureToolGroupingOptions).Build();

        var request = new EnumeratedOnceEnumerable<ChatMessage>(scenario.InitialMessages);
        ChatResponse response = await client.GetResponseAsync(request, scenario.Options, CancellationToken.None);

        Assert.Equal(turns.Count, iteration);

        if (generatedUsage.Count == 0)
        {
            Assert.Null(response.Usage);
        }
        else
        {
            AssertAggregatedUsage(response.Usage, generatedUsage.Select(CloneUsage).ToArray());
        }

        return new ToolGroupingTestResult(response);
    }

    private static async Task<ToolGroupingTestResult> InvokeAndAssertStreamingAsync(ToolGroupingTestScenario scenario)
    {
        if (scenario.InitialMessages.Count == 0)
        {
            throw new InvalidOperationException("Scenario must include at least one initial message.");
        }

        List<DownstreamTurn> turns = scenario.Turns;
        int iteration = 0;
        List<UsageDetails> generatedUsage = [];

        using TestChatClient inner = new();

        inner.GetStreamingResponseAsyncCallback = (messages, options, cancellationToken) =>
        {
            var materialized = messages.ToList();
            if (iteration >= turns.Count)
            {
                throw new InvalidOperationException("Unexpected additional iteration.");
            }

            var turn = turns[iteration];
            turn.AssertInvocation?.Invoke(new ToolGroupingInvocationContext(iteration, materialized, options));

            UsageDetails usage = CreateRandomUsage();
            generatedUsage.Add(usage);

            var response = CreateResponse(turn, usage);
            iteration++;
            return YieldAsync(response.ToChatResponseUpdates());
        };

        using IChatClient client = inner.AsBuilder().UseToolGrouping(scenario.ConfigureToolGroupingOptions).Build();

        var request = new EnumeratedOnceEnumerable<ChatMessage>(scenario.InitialMessages);
        ChatResponse response = await client.GetStreamingResponseAsync(request, scenario.Options, CancellationToken.None).ToChatResponseAsync();

        Assert.Equal(turns.Count, iteration);

        if (generatedUsage.Count == 0)
        {
            Assert.Null(response.Usage);
        }
        else
        {
            AssertAggregatedUsage(response.Usage, generatedUsage.Select(CloneUsage).ToArray());
        }

        return new ToolGroupingTestResult(response);
    }

    private static UsageDetails CreateRandomUsage()
    {
        int value;
        lock (_randomLock)
        {
            value = _random.Next(1, 100);
        }

        return new UsageDetails
        {
            InputTokenCount = value,
            OutputTokenCount = value,
            TotalTokenCount = value,
            AdditionalCounts = new AdditionalPropertiesDictionary<long>
            {
                ["alpha"] = value,
                ["beta"] = value,
            }
        };
    }

    private static ChatResponse CreateResponse(DownstreamTurn turn, UsageDetails? usage = null)
    {
        ChatResponse response = new ChatResponse(CloneMessage(turn.ResponseMessage));
        if (turn.ConversationId is not null)
        {
            response.ConversationId = turn.ConversationId;
        }

        if (usage is not null)
        {
            response.Usage = CloneUsage(usage);
        }

        return response;
    }

    private static ChatMessage CreateExpansionCall(string callId, string groupName) =>
        new(ChatRole.Assistant, [new FunctionCallContent(callId, ExpansionToolName, new Dictionary<string, object?> { ["groupName"] = groupName })]);

    private static void AssertContainsResultMessage(ChatResponse response, string substring)
    {
        var toolMessages = response.Messages.Where(m => m.Role == ChatRole.Tool).ToList();
        Assert.NotEmpty(toolMessages);
        Assert.Contains(toolMessages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()), r =>
        {
            var text = r.Result?.ToString() ?? string.Empty;
            return text.Contains(substring);
        });
    }

    private static void AssertAggregatedUsage(UsageDetails? actual, params UsageDetails[] expected)
    {
        Assert.NotNull(actual);
        long expectedInput = expected.Sum(u => u.InputTokenCount ?? 0);
        long expectedOutput = expected.Sum(u => u.OutputTokenCount ?? 0);

        Assert.Equal(expectedInput, actual!.InputTokenCount);
        Assert.Equal(expectedOutput, actual.OutputTokenCount);

        if (expected.Any(u => u.TotalTokenCount is not null))
        {
            long expectedTotal = expected.Sum(u => u.TotalTokenCount ?? 0);
            Assert.Equal(expectedTotal, actual.TotalTokenCount);
        }

        Dictionary<string, long> aggregated = [];
        foreach (var usage in expected)
        {
            if (usage.AdditionalCounts is null)
            {
                continue;
            }

            foreach (var kvp in usage.AdditionalCounts)
            {
                aggregated[kvp.Key] = aggregated.TryGetValue(kvp.Key, out long value) ? value + kvp.Value : kvp.Value;
            }
        }

        if (aggregated.Count == 0)
        {
            Assert.True(actual.AdditionalCounts is null || actual.AdditionalCounts.Count == 0);
        }
        else
        {
            Assert.NotNull(actual.AdditionalCounts);
            foreach (var kvp in aggregated)
            {
                Assert.True(actual.AdditionalCounts!.TryGetValue(kvp.Key, out long value));
                Assert.Equal(kvp.Value, value);
            }
        }
    }

    private static ChatMessage CloneMessage(ChatMessage message)
    {
        List<AIContent> contents = message.Contents.Select(CloneContent).ToList();
        ChatMessage clone = new(message.Role, contents)
        {
            AuthorName = message.AuthorName,
        };

        if (message.AdditionalProperties is { Count: > 0 } additionalProperties)
        {
            clone.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            foreach (var property in additionalProperties)
            {
                clone.AdditionalProperties[property.Key] = property.Value;
            }
        }

        clone.MessageId = Guid.NewGuid().ToString("N");
        return clone;
    }

    private static AIContent CloneContent(AIContent content) => content switch
    {
        TextContent text => CloneTextContent(text),
        FunctionCallContent call => new FunctionCallContent(call.CallId, call.Name,
            call.Arguments is null ? null : new Dictionary<string, object?>(call.Arguments)),
        FunctionResultContent result => new FunctionResultContent(result.CallId, result.Result),
        _ => throw new NotSupportedException($"Unsupported content type: {content.GetType()}")
    };

    private static TextContent CloneTextContent(TextContent content)
    {
        TextContent clone = new(content.Text)
        {
            RawRepresentation = content.RawRepresentation,
        };

        if (content.AdditionalProperties is { Count: > 0 } additionalProperties)
        {
            clone.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            foreach (var property in additionalProperties)
            {
                clone.AdditionalProperties[property.Key] = property.Value;
            }
        }

        return clone;
    }

    private static UsageDetails CloneUsage(UsageDetails usage)
    {
        UsageDetails clone = new()
        {
            InputTokenCount = usage.InputTokenCount,
            OutputTokenCount = usage.OutputTokenCount,
            TotalTokenCount = usage.TotalTokenCount,
        };

        if (usage.AdditionalCounts is { Count: > 0 })
        {
            clone.AdditionalCounts = new AdditionalPropertiesDictionary<long>(usage.AdditionalCounts);
        }

        return clone;
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldAsync(IEnumerable<ChatResponseUpdate> updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private sealed class SimpleTool : AITool
    {
        private readonly string _name;
        private readonly string _description;

        public SimpleTool(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public override string Name => _name;
        public override string Description => _description;
    }

    private sealed class TestChatClient : IChatClient
    {
        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? GetResponseAsyncCallback { get; set; }
        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? GetStreamingResponseAsyncCallback { get; set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (GetResponseAsyncCallback is null)
            {
                throw new NotSupportedException();
            }

            return GetResponseAsyncCallback(messages, options, cancellationToken);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (GetStreamingResponseAsyncCallback is null)
            {
                throw new NotSupportedException();
            }

            return GetStreamingResponseAsyncCallback(messages, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
            // No-op
        }
    }

    private sealed class ToolGroupingTestScenario
    {
        public List<ChatMessage> InitialMessages { get; init; } = [];
        public Action<ToolGroupingOptions> ConfigureToolGroupingOptions { get; init; } = _ => { };
        public List<DownstreamTurn> Turns { get; init; } = [];
        public ChatOptions? Options { get; init; }
    }

    private sealed class DownstreamTurn
    {
        public ChatMessage ResponseMessage { get; init; } = new(ChatRole.Assistant, string.Empty);
        public string? ConversationId { get; init; }
        public Action<ToolGroupingInvocationContext>? AssertInvocation { get; init; }
    }

    private sealed record ToolGroupingInvocationContext(int Iteration, IReadOnlyList<ChatMessage> Messages, ChatOptions? Options);

    private sealed record ToolGroupingTestResult(ChatResponse Response);

    [Fact]
    public async Task ToolGroupingChatClient_NestedGroups_ExpandsParentThenChild()
    {
        var nestedTool1 = new SimpleTool("NestedTool1", "nested tool 1");
        var nestedTool2 = new SimpleTool("NestedTool2", "nested tool 2");
        var parentTool = new SimpleTool("ParentTool", "parent tool");
        var ungrouped = new SimpleTool("Ungrouped", "ungrouped");

        var nestedGroup = AIToolGroup.Create("NestedGroup", "Nested group", [nestedTool1, nestedTool2]);
        var parentGroup = AIToolGroup.Create("ParentGroup", "Parent group", [parentTool, nestedGroup]);

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "expand parent then nested")],
            Options = new ChatOptions { Tools = [ungrouped, parentGroup] },
            ConfigureToolGroupingOptions = options => options.MaxExpansionsPerRequest = 2,
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "ParentGroup"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call2", "NestedGroup"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed) => Assert.Collection(observed,
            tools =>
            {
                // First iteration: collapsed state
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
                Assert.DoesNotContain(tools, t => t.Name == parentTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == nestedTool1.Name);
            },
            tools =>
            {
                // Second iteration: parent expanded
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
                Assert.Contains(tools, t => t.Name == parentTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == nestedTool1.Name);

                // NestedGroup should NOT be in tools list (only actual tools)
                Assert.DoesNotContain(tools, t => t.Name == "NestedGroup");
            },
            tools =>
            {
                // Third iteration: nested group expanded
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
                Assert.Contains(tools, t => t.Name == nestedTool1.Name);
                Assert.Contains(tools, t => t.Name == nestedTool2.Name);
                Assert.DoesNotContain(tools, t => t.Name == parentTool.Name);
            });

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'ParentGroup'");
            AssertContainsResultMessage(testResult.Response, "Additional groups available for expansion");
            AssertContainsResultMessage(testResult.Response, "- NestedGroup:");
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'NestedGroup'");
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_NestedGroups_CannotExpandNestedFromDifferentParent()
    {
        var toolA = new SimpleTool("ToolA", "tool a");
        var nestedATool = new SimpleTool("NestedATool", "nested a tool");
        var nestedA = AIToolGroup.Create("NestedA", "Nested A", [nestedATool]);
        var groupA = AIToolGroup.Create("GroupA", "Group A", [toolA, nestedA]);

        var toolB = new SimpleTool("ToolB", "tool b");
        var nestedBTool = new SimpleTool("NestedBTool", "nested b tool");
        var nestedB = AIToolGroup.Create("NestedB", "Nested B", [nestedBTool]);
        var groupB = AIToolGroup.Create("GroupB", "Group B", [toolB, nestedB]);

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "try to expand wrong nested group")],
            Options = new ChatOptions { Tools = [groupA, groupB] },
            ConfigureToolGroupingOptions = options => options.MaxExpansionsPerRequest = 2,
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "GroupA") },
                new DownstreamTurn
                {
                    // Try to expand NestedB (belongs to unexpanded GroupB) - should fail
                    ResponseMessage = CreateExpansionCall("call2", "NestedB")
                }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'GroupA'");
            AssertContainsResultMessage(testResult.Response, "group name 'NestedB' was invalid");
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_NestedGroups_MultiLevelNesting()
    {
        var deeplyNestedTool = new SimpleTool("DeeplyNestedTool", "deeply nested tool");
        var deeplyNested = AIToolGroup.Create("DeeplyNested", "Deeply nested group", [deeplyNestedTool]);

        var nestedTool = new SimpleTool("NestedTool", "nested tool");
        var nested = AIToolGroup.Create("Nested", "Nested group", [nestedTool, deeplyNested]);

        var topTool = new SimpleTool("TopTool", "top tool");
        var topGroup = AIToolGroup.Create("TopGroup", "Top group", [topTool, nested]);

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "three level nesting")],
            Options = new ChatOptions { Tools = [topGroup] },
            ConfigureToolGroupingOptions = options => options.MaxExpansionsPerRequest = 3,
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "TopGroup"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call2", "Nested"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call3", "DeeplyNested"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed) => Assert.Collection(observed,
            tools =>
            {
                // Collapsed: only expansion function
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ExpansionToolName);
                Assert.DoesNotContain(tools, t => t.Name == topTool.Name);
            },
            tools =>
            {
                // TopGroup expanded: topTool + Nested available
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == topTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == nestedTool.Name);
            },
            tools =>
            {
                // Nested expanded: nestedTool + DeeplyNested available
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == nestedTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == topTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == deeplyNestedTool.Name);
            },
            tools =>
            {
                // DeeplyNested expanded: only deeplyNestedTool
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == deeplyNestedTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == nestedTool.Name);
            });

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'TopGroup'");
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'Nested'");
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'DeeplyNested'");
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_NestedGroups_EmptyNestedGroupsList()
    {
        var tool1 = new SimpleTool("Tool1", "tool 1");
        var tool2 = new SimpleTool("Tool2", "tool 2");
        var group = AIToolGroup.Create("Group", "Group with no nested groups", [tool1, tool2]);

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "expand group with no nested groups")],
            Options = new ChatOptions { Tools = [group] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "Group") },
                new DownstreamTurn { ResponseMessage = new(ChatRole.Assistant, "done") }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'Group'");

            // Should NOT contain "Additional groups available"
            var toolMessages = testResult.Response.Messages.Where(m => m.Role == ChatRole.Tool).ToList();
            var resultContents = toolMessages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()).ToList();
            var hasAdditionalGroupsMessage = resultContents.Any(r => r.Result?.ToString()?.Contains("Additional groups available") ?? false);
            Assert.False(hasAdditionalGroupsMessage, "Should not mention 'Additional groups available' when there are no nested groups");
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ToolNameCollision_WithExpansionFunction()
    {
        var collisionTool = new SimpleTool("__expand_tool_group", "collision");
        var group = AIToolGroup.Create("Group", "group", [new SimpleTool("Tool", "tool")]);

        using TestChatClient inner = new();
        using IChatClient client = inner.AsBuilder().UseToolGrouping(_ => { }).Build();

        var options = new ChatOptions { Tools = [collisionTool, group] };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            try
            {
                await client.GetResponseAsync([new ChatMessage(ChatRole.User, "test")], options);
            }
            catch (NotSupportedException)
            {
                // Inner client throws NotSupportedException, but we should hit InvalidOperationException first
                throw;
            }
        });

        Assert.Contains("__expand_tool_group", exception.Message);
        Assert.Contains("collides", exception.Message);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ToolNameCollision_WithListGroupsFunction()
    {
        var collisionTool = new SimpleTool("__list_tool_groups", "collision");
        var group = AIToolGroup.Create("Group", "group", [new SimpleTool("Tool", "tool")]);

        using TestChatClient inner = new();
        using IChatClient client = inner.AsBuilder().UseToolGrouping(_ => { }).Build();

        var options = new ChatOptions { Tools = [collisionTool, group] };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            try
            {
                await client.GetResponseAsync([new ChatMessage(ChatRole.User, "test")], options);
            }
            catch (NotSupportedException)
            {
                throw;
            }
        });

        Assert.Contains("__list_tool_groups", exception.Message);
        Assert.Contains("collides", exception.Message);
    }

    [Fact]
    public async Task ToolGroupingChatClient_DynamicToolGroup_GetToolsAsyncCalled()
    {
        var tool = new SimpleTool("DynamicTool", "dynamic tool");
        bool getToolsAsyncCalled = false;

        var dynamicGroup = new DynamicToolGroup(
            "DynamicGroup",
            "Dynamic group",
            async ct =>
            {
                getToolsAsyncCalled = true;
                await Task.Yield();
                return [tool];
            });

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "expand dynamic group")],
            Options = new ChatOptions { Tools = [dynamicGroup] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "DynamicGroup") },
                new DownstreamTurn { ResponseMessage = new(ChatRole.Assistant, "done") }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());

        Assert.True(getToolsAsyncCalled, "GetToolsAsync should have been called");
        AssertContainsResultMessage(result.Response, "Successfully expanded group 'DynamicGroup'");

        // Reset for streaming test
        getToolsAsyncCalled = false;
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        Assert.True(getToolsAsyncCalled, "GetToolsAsync should have been called in streaming");
        AssertContainsResultMessage(streamingResult.Response, "Successfully expanded group 'DynamicGroup'");
    }

    [Fact]
    public async Task ToolGroupingChatClient_DynamicToolGroup_ReturnsNestedGroups()
    {
        var nestedTool = new SimpleTool("NestedTool", "nested tool");
        var nestedGroup = AIToolGroup.Create("NestedGroup", "Nested group", [nestedTool]);
        var regularTool = new SimpleTool("RegularTool", "regular tool");

        var dynamicGroup = new DynamicToolGroup(
            "DynamicGroup",
            "Dynamic group with nested",
            async ct =>
            {
                await Task.Yield();
                return [regularTool, nestedGroup];
            });

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "expand dynamic with nested")],
            Options = new ChatOptions { Tools = [dynamicGroup] },
            ConfigureToolGroupingOptions = options => options.MaxExpansionsPerRequest = 2,
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "DynamicGroup"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call2", "NestedGroup"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed) => Assert.Collection(observed,
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ExpansionToolName);
            },
            tools =>
            {
                // After expanding DynamicGroup
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == regularTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == "NestedGroup");
                Assert.DoesNotContain(tools, t => t.Name == nestedTool.Name);
            },
            tools =>
            {
                // After expanding NestedGroup
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == nestedTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == regularTool.Name);
            });

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'DynamicGroup'");
            AssertContainsResultMessage(testResult.Response, "Additional groups available for expansion");
            AssertContainsResultMessage(testResult.Response, "- NestedGroup:");
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_DynamicToolGroup_ThrowsException()
    {
        var dynamicGroup = new DynamicToolGroup(
            "FailingGroup",
            "Failing group",
            ct => throw new InvalidOperationException("Simulated failure"));

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "expand failing group")],
            Options = new ChatOptions { Tools = [dynamicGroup] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "FailingGroup") }
            ]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await InvokeAndAssertAsync(CreateScenario()));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await InvokeAndAssertStreamingAsync(CreateScenario()));
    }

    [Fact]
    public async Task ToolGroupingChatClient_EmptyGroupExpansion_ReturnsNoTools()
    {
        var emptyGroup = AIToolGroup.Create("EmptyGroup", "Empty group", []);
        var ungroupedTool = new SimpleTool("Ungrouped", "ungrouped");

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "expand empty group")],
            Options = new ChatOptions { Tools = [ungroupedTool, emptyGroup] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "EmptyGroup"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed) => Assert.Collection(observed,
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungroupedTool.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);
            },
            tools =>
            {
                // After expanding empty group, only ungrouped tool + expansion/list functions remain
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungroupedTool.Name);
                Assert.Contains(tools, t => t.Name == ExpansionToolName);

                // No group-specific tools should be added
                Assert.Equal(3, tools.Count); // ungrouped + expansion + list
            });

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'EmptyGroup'");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_AllToolsAreGroups_NoUngroupedTools()
    {
        var toolA = new SimpleTool("ToolA", "tool a");
        var groupA = AIToolGroup.Create("GroupA", "Group A", [toolA]);

        var toolB = new SimpleTool("ToolB", "tool b");
        var groupB = AIToolGroup.Create("GroupB", "Group B", [toolB]);

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "only groups")],
            Options = new ChatOptions { Tools = [groupA, groupB] },
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "ok"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed)
        {
            var tools = Assert.Single(observed);
            Assert.NotNull(tools);

            // Should only contain expansion and list functions
            Assert.Contains(tools!, t => t.Name == ExpansionToolName);
            Assert.Contains(tools, t => t.Name == "__list_tool_groups");
            Assert.DoesNotContain(tools, t => t.Name == toolA.Name);
            Assert.DoesNotContain(tools, t => t.Name == toolB.Name);
            Assert.Equal(2, tools.Count);
        }

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);
    }

    [Fact]
    public async Task ToolGroupingChatClient_NullToolsList_BypassesMiddleware()
    {
        List<ChatOptions?> observedOptions = [];

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test")],
            Options = null,
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "ok"),
                    AssertInvocation = ctx => observedOptions.Add(ctx.Options)
                }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        Assert.Equal("ok", result.Response.Text);
        Assert.Equal("ok", streamingResult.Response.Text);

        // Options should be null (bypassed)
        Assert.Contains(observedOptions, o => o is null);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ExpansionWithHighIterationLimit()
    {
        var tool1 = new SimpleTool("Tool1", "tool 1");
        var group1 = AIToolGroup.Create("Group1", "Group 1", [tool1]);

        var tool2 = new SimpleTool("Tool2", "tool 2");
        var group2 = AIToolGroup.Create("Group2", "Group 2", [tool2]);

        var tool3 = new SimpleTool("Tool3", "tool 3");
        var group3 = AIToolGroup.Create("Group3", "Group 3", [tool3]);

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "multiple expansions")],
            Options = new ChatOptions { Tools = [group1, group2, group3] },
            ConfigureToolGroupingOptions = options => options.MaxExpansionsPerRequest = 10,
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "Group1") },
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call2", "Group2") },
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call3", "Group3") },
                new DownstreamTurn { ResponseMessage = new(ChatRole.Assistant, "done") }
            ]
        };

        var result = await InvokeAndAssertAsync(CreateScenario());
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario());

        void AssertResponse(ToolGroupingTestResult testResult)
        {
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'Group1'");
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'Group2'");
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'Group3'");
            Assert.Equal("done", testResult.Response.Text);
        }

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_OptionsCloning_DoesNotMutateOriginal()
    {
        var tool = new SimpleTool("Tool", "tool");
        var group = AIToolGroup.Create("Group", "group", [tool]);

        var originalOptions = new ChatOptions { Tools = [group] };
        var originalToolsCount = originalOptions.Tools.Count;

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test")],
            Options = originalOptions,
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn { ResponseMessage = new(ChatRole.Assistant, "ok") }
            ]
        };

        await InvokeAndAssertAsync(CreateScenario());

        // Verify original options unchanged
        Assert.Equal(originalToolsCount, originalOptions.Tools.Count);
        Assert.Contains(originalOptions.Tools, t => t is AIToolGroup);
    }

    [Fact]
    public async Task ToolGroupingChatClient_AdditionalProperties_PreservedAcrossIterations()
    {
        var tool = new SimpleTool("Tool", "tool");
        var group = AIToolGroup.Create("Group", "group", [tool]);

        var options = new ChatOptions
        {
            Tools = [group],
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                ["custom_key"] = "custom_value",
                ["number"] = 42
            }
        };

        List<AdditionalPropertiesDictionary?> observedProperties = [];

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test")],
            Options = options,
            ConfigureToolGroupingOptions = _ => { },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = CreateExpansionCall("call1", "Group"),
                    AssertInvocation = ctx => observedProperties.Add(ctx.Options?.AdditionalProperties)
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedProperties.Add(ctx.Options?.AdditionalProperties)
                }
            ]
        };

        await InvokeAndAssertAsync(CreateScenario());

        // Both iterations should preserve additional properties
        Assert.All(observedProperties, props =>
        {
            Assert.NotNull(props);
            Assert.Equal("custom_value", props!["custom_key"]);
            Assert.Equal(42, props["number"]);
        });
    }

    [Fact]
    public async Task ToolGroupingChatClient_CustomExpansionFunctionName()
    {
        var tool = new SimpleTool("Tool", "tool");
        var group = AIToolGroup.Create("Group", "group", [tool]);

        const string CustomExpansionName = "my_custom_expand";

        ToolGroupingTestScenario CreateScenario(List<IList<AITool>?> observedTools) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test custom name")],
            Options = new ChatOptions { Tools = [group] },
            ConfigureToolGroupingOptions = options =>
            {
                options.ExpansionFunctionName = CustomExpansionName;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent("call1", CustomExpansionName, new Dictionary<string, object?> { ["groupName"] = "Group" })]),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "done"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        List<IList<AITool>?> observedNonStreaming = [];
        List<IList<AITool>?> observedStreaming = [];

        var result = await InvokeAndAssertAsync(CreateScenario(observedNonStreaming));
        var streamingResult = await InvokeAndAssertStreamingAsync(CreateScenario(observedStreaming));

        void AssertObservedTools(List<IList<AITool>?> observed)
        {
            Assert.Equal(2, observed.Count);

            // All iterations should have custom expansion function
            Assert.All(observed, tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == CustomExpansionName);
                Assert.DoesNotContain(tools, t => t.Name == ExpansionToolName);
            });
        }

        AssertObservedTools(observedNonStreaming);
        AssertObservedTools(observedStreaming);

        void AssertResponse(ToolGroupingTestResult testResult) =>
            AssertContainsResultMessage(testResult.Response, "Successfully expanded group 'Group'");

        AssertResponse(result);
        AssertResponse(streamingResult);
    }

    [Fact]
    public async Task ToolGroupingChatClient_CustomExpansionFunctionDescription()
    {
        var tool = new SimpleTool("Tool", "tool");
        var group = AIToolGroup.Create("Group", "group", [tool]);

        const string CustomDescription = "Use this custom function to expand a tool group";

        List<IList<AITool>?> observedTools = [];

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test custom description")],
            Options = new ChatOptions { Tools = [group] },
            ConfigureToolGroupingOptions = options =>
            {
                options.ExpansionFunctionDescription = CustomDescription;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "ok"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        await InvokeAndAssertAsync(CreateScenario());

        var tools = Assert.Single(observedTools);
        Assert.NotNull(tools);
        var expansionTool = tools!.FirstOrDefault(t => t.Name == ExpansionToolName);
        Assert.NotNull(expansionTool);
        Assert.Equal(CustomDescription, expansionTool!.Description);
    }

    [Fact]
    public async Task ToolGroupingChatClient_CustomListGroupsFunctionName()
    {
        var tool = new SimpleTool("Tool", "tool");
        var group = AIToolGroup.Create("Group", "group", [tool]);

        const string CustomListName = "my_list_groups";

        List<IList<AITool>?> observedTools = [];

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test custom list name")],
            Options = new ChatOptions { Tools = [group] },
            ConfigureToolGroupingOptions = options =>
            {
                options.ListGroupsFunctionName = CustomListName;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "ok"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        await InvokeAndAssertAsync(CreateScenario());

        var tools = Assert.Single(observedTools);
        Assert.NotNull(tools);
        Assert.Contains(tools!, t => t.Name == CustomListName);
        Assert.DoesNotContain(tools, t => t.Name == "__list_tool_groups");
    }

    [Fact]
    public async Task ToolGroupingChatClient_CustomListGroupsFunctionDescription()
    {
        var tool = new SimpleTool("Tool", "tool");
        var group = AIToolGroup.Create("Group", "group", [tool]);

        const string CustomDescription = "Custom description for listing groups";

        List<IList<AITool>?> observedTools = [];

        ToolGroupingTestScenario CreateScenario() => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "test custom list description")],
            Options = new ChatOptions { Tools = [group] },
            ConfigureToolGroupingOptions = options =>
            {
                options.ListGroupsFunctionDescription = CustomDescription;
            },
            Turns =
            [
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "ok"),
                    AssertInvocation = ctx => observedTools.Add(ctx.Options?.Tools?.ToList()),
                }
            ]
        };

        await InvokeAndAssertAsync(CreateScenario());

        var tools = Assert.Single(observedTools);
        Assert.NotNull(tools);
        var listTool = tools!.FirstOrDefault(t => t.Name == "__list_tool_groups");
        Assert.NotNull(listTool);
        Assert.Equal(CustomDescription, listTool!.Description);
    }

    private sealed class DynamicToolGroup : AIToolGroup
    {
        private readonly Func<CancellationToken, Task<IEnumerable<AITool>>> _getToolsFunc;

        public DynamicToolGroup(string name, string description, Func<CancellationToken, Task<IEnumerable<AITool>>> getToolsFunc)
            : base(name, description)
        {
            _getToolsFunc = getToolsFunc;
        }

        public override async ValueTask<IReadOnlyList<AITool>> GetToolsAsync(CancellationToken cancellationToken = default)
        {
            var tools = await _getToolsFunc(cancellationToken);
            return tools.ToList();
        }
    }

    private sealed class EnumeratedOnceEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _items;
        private bool _enumerated;

        public EnumeratedOnceEnumerable(IEnumerable<T> items)
        {
            _items = items;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_enumerated)
            {
                throw new InvalidOperationException("Sequence may only be enumerated once.");
            }

            _enumerated = true;
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
