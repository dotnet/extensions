// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ToolGroupingTests
{
    [Fact]
    public async Task ToolGroupingChatClient_Collapsed_IncludesExpansionAndUngroupedOnly()
    {
        var ungrouped = new SimpleTool("Basic", "basic");
        var groupedA = new SimpleTool("A1", "a1");
        var groupedB = new SimpleTool("B1", "b1");

        IList<AITool>? observedTools = null;
        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, ct) =>
            {
                observedTools = options?.Tools;
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "Hi") }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                o.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
            })
            .Build();

        await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hello") }, new ChatOptions { Tools = [ungrouped, groupedA, groupedB] });

        Assert.NotNull(observedTools);
        Assert.Contains(observedTools!, t => t.Name == "Basic");
        Assert.DoesNotContain(observedTools!, t => t.Name == "A1");
        Assert.DoesNotContain(observedTools!, t => t.Name == "B1");
        Assert.Contains(observedTools!, t => t.Name == "__expand_tool_group");
    }

    [Fact]
    public async Task ToolGroupingChatClient_ExpansionLoop_ExpandsSingleGroup()
    {
        var groupedA1 = new SimpleTool("A1", "a1");
        var groupedA2 = new SimpleTool("A2", "a2");
        var groupedB = new SimpleTool("B1", "b1");
        var ungrouped = new SimpleTool("Common", "c");

        int callIndex = 0;
        IList<AITool>? firstObserved = null;
        IList<AITool>? secondObserved = null;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, ct) =>
            {
                if (callIndex == 0)
                {
                    firstObserved = options?.Tools;

                    // Simulate model requesting expansion of GroupA
                    var fcc = new FunctionCallContent(callId: Guid.NewGuid().ToString("N"), name: "__expand_tool_group", arguments: new Dictionary<string, object?> { ["groupName"] = "GroupA" });
                    callIndex++;
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [fcc]) }));
                }
                else
                {
                    secondObserved = options?.Tools;
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "Done") }));
                }
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA1, groupedA2 }));
                o.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
                o.MaxExpansionsPerRequest = 1;
            })
            .Build();

        await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "go") }, new ChatOptions { Tools = [ungrouped, groupedA1, groupedA2, groupedB] });

        Assert.NotNull(firstObserved);
        Assert.NotNull(secondObserved);

        // First call collapsed
        Assert.DoesNotContain(firstObserved!, t => t.Name == "A1");

        // Second call expanded
        Assert.Contains(secondObserved!, t => t.Name == "A1");
        Assert.Contains(secondObserved!, t => t.Name == "A2");

        // Only one group expanded
        Assert.DoesNotContain(secondObserved!, t => t.Name == "B1");
    }

    [Fact]
    public async Task ToolGroupingChatClient_NoGroups_BypassesMiddleware()
    {
        var tool = new SimpleTool("Standalone", "s");
        int callCount = 0;
        ChatOptions? observedOptions = null;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, ct) =>
            {
                callCount++;
                observedOptions = options;
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "ok") }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o => { })
            .Build();

        ChatOptions options = new() { Tools = [tool] };
        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.Equal(1, callCount);
        Assert.Same(options, observedOptions);
        Assert.NotNull(observedOptions!.Tools);
        Assert.DoesNotContain(observedOptions.Tools, t => t.Name == "__expand_tool_group");
    }

    [Fact]
    public async Task ToolGroupingChatClient_InvalidGroupRequest_ReturnsResultMessage()
    {
        var groupedA = new SimpleTool("A1", "a1");

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                var call = new FunctionCallContent(Guid.NewGuid().ToString("N"), "__expand_tool_group", new Dictionary<string, object?>
                {
                    ["groupName"] = "Unknown"
                });

                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                o.MaxExpansionsPerRequest = 2;
            })
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        var toolMessage = Assert.Single(response.Messages.Where(m => m.Role == ChatRole.Tool));
        var result = Assert.Single(toolMessage.Contents.OfType<FunctionResultContent>());
        var resultText = result.Result?.ToString() ?? string.Empty;
        Assert.Contains("was invalid; ignoring expansion request", resultText);
    }

    [Fact]
    public async Task ToolGroupingChatClient_MissingGroupName_ReturnsNotice()
    {
        var groupedA = new SimpleTool("A1", "a1");

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                var call = new FunctionCallContent(Guid.NewGuid().ToString("N"), "__expand_tool_group");
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                o.MaxExpansionsPerRequest = 2;
            })
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        var toolMessage = Assert.Single(response.Messages.Where(m => m.Role == ChatRole.Tool));
        var result = Assert.Single(toolMessage.Contents.OfType<FunctionResultContent>());
        var resultText = result.Result?.ToString() ?? string.Empty;
        Assert.Contains("No group name was specified", resultText);
    }

    [Fact]
    public async Task ToolGroupingChatClient_GroupNameReadsJsonElement()
    {
        var groupedA = new SimpleTool("A1", "a1");

        var callId = Guid.NewGuid().ToString("N");
        var jsonValue = JsonDocument.Parse("\"GroupA\"").RootElement;

        int callIndex = 0;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                if (callIndex++ == 0)
                {
                    var call = new FunctionCallContent(callId, "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = jsonValue
                    });

                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) }));
                }

                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "done") }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o => o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })))
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        var toolMessage = Assert.Single(response.Messages.Where(m => m.Role == ChatRole.Tool));
        var result = Assert.Single(toolMessage.Contents.OfType<FunctionResultContent>());
        var resultText = result.Result?.ToString() ?? string.Empty;
        Assert.Contains("Successfully expanded group 'GroupA'", resultText);
    }

    [Fact]
    public async Task ToolGroupingChatClient_ExpansionLimitReached_AppendsLimitMessage()
    {
        var groupedA = new SimpleTool("A1", "a1");
        var groupedB = new SimpleTool("B1", "b1");
        var alwaysOn = new SimpleTool("Common", "c");

        int callIndex = 0;
        IList<AITool>? firstIterationTools = null;
        IList<AITool>? secondIterationTools = null;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, opts, _) =>
            {
                if (callIndex == 0)
                {
                    firstIterationTools = opts?.Tools;
                    callIndex++;
                    var call = new FunctionCallContent("call1", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) }));
                }

                secondIterationTools = opts?.Tools;
                callIndex++;
                var secondCall = new FunctionCallContent("call2", "__expand_tool_group", new Dictionary<string, object?>
                {
                    ["groupName"] = "GroupB"
                });
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [secondCall]) }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                o.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
                o.MaxExpansionsPerRequest = 1;
            })
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")], new ChatOptions
        {
            Tools = [alwaysOn, groupedA, groupedB]
        });

        Assert.Equal(2, callIndex);

        Assert.NotNull(firstIterationTools);
        Assert.Contains(firstIterationTools!, t => t.Name == "__expand_tool_group");
        Assert.DoesNotContain(firstIterationTools!, t => t.Name == "A1");

        Assert.NotNull(secondIterationTools);
        Assert.Contains(secondIterationTools!, t => t.Name == "A1");
        Assert.DoesNotContain(secondIterationTools!, t => t.Name == "B1");

        var toolMessages = response.Messages.Where(m => m.Role == ChatRole.Tool).ToList();
        Assert.NotEmpty(toolMessages);
        Assert.Contains(toolMessages.SelectMany(m => m.Contents.OfType<FunctionResultContent>()), r =>
        {
            var text = r.Result?.ToString() ?? string.Empty;
            return text.Contains("Max expansion iteration count reached");
        });
    }

    [Fact]
    public async Task ToolGroupingChatClient_MultipleValidExpansions_LastWins()
    {
        var groupedA = new SimpleTool("A1", "a1");
        var groupedB = new SimpleTool("B1", "b1");
        var alwaysOn = new SimpleTool("Common", "c");

        int callIndex = 0;
        IList<AITool>? firstIterationTools = null;
        IList<AITool>? secondIterationTools = null;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, opts, _) =>
            {
                if (callIndex == 0)
                {
                    firstIterationTools = opts?.Tools;
                    callIndex++;
                    var callA = new FunctionCallContent("call1", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    var callB = new FunctionCallContent("call2", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupB"
                    });
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [callA, callB]) }));
                }

                secondIterationTools = opts?.Tools;
                callIndex++;
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "done") }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                o.Groups.Add(new AIToolGroup("GroupB", "Group B", new[] { groupedB }));
                o.MaxExpansionsPerRequest = 2;
            })
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")], new ChatOptions
        {
            Tools = [alwaysOn, groupedA, groupedB]
        });

        Assert.Equal(2, callIndex);
        Assert.NotNull(firstIterationTools);
        Assert.DoesNotContain(firstIterationTools!, t => t.Name == "A1");

        Assert.NotNull(secondIterationTools);
        Assert.Contains(secondIterationTools!, t => t.Name == "B1");
        Assert.DoesNotContain(secondIterationTools!, t => t.Name == "A1");

        var toolMessage = response.Messages.Single(m => m.Role == ChatRole.Tool);
        var results = toolMessage.Contents.OfType<FunctionResultContent>().ToList();
        Assert.Contains(results, r =>
        {
            var text = r.Result?.ToString() ?? string.Empty;
            return text.Contains("Successfully expanded group 'GroupA'");
        });
        Assert.Contains(results, r =>
        {
            var text = r.Result?.ToString() ?? string.Empty;
            return text.Contains("Successfully expanded group 'GroupB'");
        });
    }

    [Fact]
    public async Task ToolGroupingChatClient_DuplicateExpansionSameIteration_Reported()
    {
        var groupedA = new SimpleTool("A1", "a1");

        int callIndex = 0;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                if (callIndex++ == 0)
                {
                    var call1 = new FunctionCallContent("call1", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    var call2 = new FunctionCallContent("call2", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call1, call2]) }));
                }

                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "done") }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o =>
            {
                o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA }));
                o.MaxExpansionsPerRequest = 2;
            })
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        var toolMessage = response.Messages.Single(m => m.Role == ChatRole.Tool);
        var results = toolMessage.Contents.OfType<FunctionResultContent>().ToList();
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r =>
        {
            var text = r.Result?.ToString() ?? string.Empty;
            return r.CallId == "call2" && text.Contains("Ignoring duplicate expansion");
        });
    }

    [Fact]
    public async Task ToolGroupingChatClient_ReexpandingSameGroupTerminatesLoop()
    {
        var groupedA = new SimpleTool("A1", "a1");

        int callIndex = 0;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                if (callIndex == 0)
                {
                    callIndex++;
                    var call = new FunctionCallContent("call1", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) }));
                }

                callIndex++;
                var duplicate = new FunctionCallContent("call2", "__expand_tool_group", new Dictionary<string, object?>
                {
                    ["groupName"] = "GroupA"
                });
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [duplicate]) }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o => o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })))
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        Assert.Equal(2, callIndex);
        var duplicateMessage = response.Messages.Last(m => m.Role == ChatRole.Tool).Contents.OfType<FunctionResultContent>().Single(r => r.CallId == "call2");
        var duplicateText = duplicateMessage.Result?.ToString() ?? string.Empty;
        Assert.Contains("Max expansion iteration count reached", duplicateText);
    }

    [Fact]
    public async Task ToolGroupingChatClient_PropagatesConversationIdBetweenIterations()
    {
        var groupedA = new SimpleTool("A1", "a1");
        string? observedConversationId = null;
        int callIndex = 0;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, opts, _) =>
            {
                if (callIndex == 0)
                {
                    callIndex++;
                    var call = new FunctionCallContent("call1", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    var response = new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) })
                    {
                        ConversationId = "conv-1"
                    };
                    return Task.FromResult(response);
                }

                observedConversationId = opts?.ConversationId;
                callIndex++;
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "done") }));
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o => o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })))
            .Build();

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        Assert.Equal("conv-1", observedConversationId);
        Assert.Equal(2, callIndex);
    }

    [Fact]
    public async Task ToolGroupingChatClient_AggregatesUsageAcrossIterations()
    {
        var groupedA = new SimpleTool("A1", "a1");

        int callIndex = 0;

        using var inner = new TestChatClient
        {
            GetResponseAsyncCallback = (_, _, _) =>
            {
                if (callIndex == 0)
                {
                    callIndex++;
                    var call = new FunctionCallContent("call1", "__expand_tool_group", new Dictionary<string, object?>
                    {
                        ["groupName"] = "GroupA"
                    });
                    return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, [call]) })
                    {
                        Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5 }
                    });
                }

                callIndex++;
                return Task.FromResult(new ChatResponse(new List<ChatMessage> { new(ChatRole.Assistant, "done") })
                {
                    Usage = new UsageDetails { InputTokenCount = 2, OutputTokenCount = 1 }
                });
            }
        };

        using var client = inner.AsBuilder()
            .UseToolGrouping(o => o.Groups.Add(new AIToolGroup("GroupA", "Group A", new[] { groupedA })))
            .Build();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "go")]);

        Assert.Equal(2, callIndex);
        Assert.NotNull(response.Usage);
        Assert.Equal(12, response.Usage!.InputTokenCount);
        Assert.Equal(6, response.Usage.OutputTokenCount);
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
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            (GetResponseAsyncCallback ?? throw new InvalidOperationException())(messages, options, cancellationToken);
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
            // No-op
        }
    }
}
