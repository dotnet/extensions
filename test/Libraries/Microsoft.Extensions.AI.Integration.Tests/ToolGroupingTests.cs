// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ToolGroupingTests
{
    private const string DefaultExpansionFunctionName = "__expand_tool_group";
    private const string DefaultListGroupsFunctionName = "__list_tool_groups";

    [Fact]
    public async Task ToolGroupingChatClient_Collapsed_IncludesUtilityAndUngroupedToolsOnly()
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
            Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
            Assert.Contains(tools, t => t.Name == DefaultListGroupsFunctionName);
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
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
                Assert.DoesNotContain(tools, t => t.Name == groupedA1.Name);
                Assert.DoesNotContain(tools, t => t.Name == groupedB.Name);
            },
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
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
            Assert.DoesNotContain(tools!, t => t.Name == DefaultExpansionFunctionName);
            Assert.DoesNotContain(tools!, t => t.Name == DefaultListGroupsFunctionName);
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
                    [new FunctionCallContent(Guid.NewGuid().ToString("N"), DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = "Unknown" })])
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "Oops!"),
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
                    [new FunctionCallContent(Guid.NewGuid().ToString("N"), DefaultExpansionFunctionName)])
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "Oops!"),
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
                    [new FunctionCallContent(Guid.NewGuid().ToString("N"), DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = jsonValue })])
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
                        new FunctionCallContent("call1", DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = "GroupA" }),
                        new FunctionCallContent("call2", DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = "GroupB" })
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
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
            },
            tools =>
            {
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == alwaysOn.Name);
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
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
                        new FunctionCallContent("call1", DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = "GroupA" }),
                        new FunctionCallContent("call2", DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = "GroupA" })
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
    public async Task ToolGroupingChatClient_ReexpandingSameGroupDoesNotTerminateLoop()
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
                new DownstreamTurn { ResponseMessage = CreateExpansionCall("call2", "GroupA") },
                new DownstreamTurn { ResponseMessage = new ChatMessage(ChatRole.Assistant, "Oops!") }
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
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
                Assert.DoesNotContain(tools, t => t.Name == parentTool.Name);
                Assert.DoesNotContain(tools, t => t.Name == nestedTool1.Name);
            },
            tools =>
            {
                // Second iteration: parent expanded
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungrouped.Name);
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
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
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
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
                },
                new DownstreamTurn
                {
                    ResponseMessage = new(ChatRole.Assistant, "Oops!"),
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
                Assert.Contains(tools!, t => t.Name == DefaultExpansionFunctionName);
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
    public async Task ToolGroupingChatClient_ToolNameCollision_WithExpansionFunction()
    {
        var collisionTool = new SimpleTool(DefaultExpansionFunctionName, "collision");
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

        Assert.Contains(DefaultExpansionFunctionName, exception.Message);
        Assert.Contains("collides", exception.Message);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ToolNameCollision_WithListGroupsFunction()
    {
        var collisionTool = new SimpleTool(DefaultListGroupsFunctionName, "collision");
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

        Assert.Contains(DefaultListGroupsFunctionName, exception.Message);
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
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);
            },
            tools =>
            {
                // After expanding empty group, only ungrouped tool + expansion/list functions remain
                Assert.NotNull(tools);
                Assert.Contains(tools!, t => t.Name == ungroupedTool.Name);
                Assert.Contains(tools, t => t.Name == DefaultExpansionFunctionName);

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
                Assert.DoesNotContain(tools, t => t.Name == DefaultExpansionFunctionName);
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
        var expansionTool = tools!.FirstOrDefault(t => t.Name == DefaultExpansionFunctionName);
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
        Assert.DoesNotContain(tools, t => t.Name == DefaultListGroupsFunctionName);
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
        var listTool = tools!.FirstOrDefault(t => t.Name == DefaultListGroupsFunctionName);
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

    private static async Task<ToolGroupingTestResult> InvokeAndAssertAsync(ToolGroupingTestScenario scenario)
    {
        if (scenario.InitialMessages.Count == 0)
        {
            throw new InvalidOperationException("Scenario must include at least one initial message.");
        }

        List<DownstreamTurn> turns = scenario.Turns;
        long expectedTotalTokenCounts = 0;
        int iteration = 0;

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
            expectedTotalTokenCounts += usage.InputTokenCount!.Value;

            var response = new ChatResponse(turn.ResponseMessage)
            {
                Usage = usage,
                ConversationId = turn.ConversationId,
            };
            iteration++;
            return Task.FromResult(response);
        };

        using IChatClient client = inner.AsBuilder().UseToolGrouping(scenario.ConfigureToolGroupingOptions).Build();

        var request = new EnumeratedOnceEnumerable<ChatMessage>(scenario.InitialMessages);
        ChatResponse response = await client.GetResponseAsync(request, scenario.Options, CancellationToken.None);

        Assert.Equal(turns.Count, iteration);

        // Usage should be aggregated over all responses, including AdditionalUsage
        var actualUsage = response.Usage!;
        Assert.Equal(expectedTotalTokenCounts, actualUsage.InputTokenCount);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.OutputTokenCount);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.TotalTokenCount);
        Assert.Equal(2, actualUsage.AdditionalCounts!.Count);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.AdditionalCounts["firstValue"]);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.AdditionalCounts["secondValue"]);

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

            var response = new ChatResponse(turn.ResponseMessage)
            {
                ConversationId = turn.ConversationId,
            };
            iteration++;
            return YieldAsync(response.ToChatResponseUpdates());
        };

        using IChatClient client = inner.AsBuilder().UseToolGrouping(scenario.ConfigureToolGroupingOptions).Build();

        var request = new EnumeratedOnceEnumerable<ChatMessage>(scenario.InitialMessages);
        ChatResponse response = await client.GetStreamingResponseAsync(request, scenario.Options, CancellationToken.None).ToChatResponseAsync();

        Assert.Equal(turns.Count, iteration);

        return new ToolGroupingTestResult(response);
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

    private static ChatMessage CreateExpansionCall(string callId, string groupName) =>
        new(ChatRole.Assistant, [new FunctionCallContent(callId, DefaultExpansionFunctionName, new Dictionary<string, object?> { ["groupName"] = groupName })]);

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
