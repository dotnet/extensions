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
            Options = new ChatOptions { Tools = [ungrouped, groupedA, groupedB] },
            ConfigureToolGroupingOptions = options =>
            {
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                options.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
            },
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
            Options = new ChatOptions { Tools = [ungrouped, groupedA1, groupedA2, groupedB] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 1;
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA1, groupedA2 }));
                options.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
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
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
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
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
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
            ConfigureToolGroupingOptions = options => options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })),
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
            Options = new ChatOptions { Tools = [alwaysOn, groupedA, groupedB] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 1;
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                options.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
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
            Options = new ChatOptions { Tools = [alwaysOn, groupedA, groupedB] },
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                options.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
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
            ConfigureToolGroupingOptions = options =>
            {
                options.MaxExpansionsPerRequest = 2;
                options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
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
            ConfigureToolGroupingOptions = options => options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })),
            Turns =
            [
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call1", "GroupA") },
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call2", "GroupA") }
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
    public async Task ToolGroupingChatClient_PropagatesConversationIdBetweenIterations()
    {
        var groupedA = new SimpleTool("A1", "a1");

        ToolGroupingTestScenario CreateScenario(List<string?> observedConversationIds) => new()
        {
            InitialMessages = [new ChatMessage(ChatRole.User, "go")],
            ConfigureToolGroupingOptions = options => options.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })),
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
