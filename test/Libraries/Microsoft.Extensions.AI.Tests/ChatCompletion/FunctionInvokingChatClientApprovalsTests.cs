// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI.ChatCompletion;

public class FunctionInvokingChatClientApprovalsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllFunctionCallsReplacedWithApprovalsWhenAllRequireApprovalAsync(bool useAdditionalTools)
    {
        AITool[] tools =
        [
            new ApprovalRequiredAIFunction(
                AIFunctionFactory.Create(() => "Result 1", "Func1")),
            new ApprovalRequiredAIFunction(
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2")),
        ];

        var options = new ChatOptions
        {
            Tools = useAdditionalTools ? null : tools
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
        ];

        List<ChatMessage> expectedOutput =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ])
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, expectedOutput, additionalTools: useAdditionalTools ? tools : null);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, expectedOutput, additionalTools: useAdditionalTools ? tools : null);
    }

    [Fact]
    public async Task AllFunctionCallsReplacedWithApprovalsWhenAnyRequireApprovalAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
        ];

        List<ChatMessage> expectedOutput =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ])
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, expectedOutput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, expectedOutput);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllFunctionCallsReplacedWithApprovalsWhenAnyRequestOrAdditionalRequireApprovalAsync(bool additionalToolsRequireApproval)
    {
        AIFunction func1 = AIFunctionFactory.Create(() => "Result 1", "Func1");
        AIFunction func2 = AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2");
        AITool[] additionalTools =
        [
            additionalToolsRequireApproval ? new ApprovalRequiredAIFunction(func1) : func1,
        ];

        var options = new ChatOptions
        {
            Tools =
            [
                additionalToolsRequireApproval ? func2 : new ApprovalRequiredAIFunction(func2),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
        ];

        List<ChatMessage> expectedOutput =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ])
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, expectedOutput, additionalTools: additionalTools);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, expectedOutput, additionalTools: additionalTools);
    }

    [Fact]
    public async Task ApprovedApprovalResponsesAreExecutedAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task ApprovedApprovalResponsesAreGroupedWhenMessageIdIsNullAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2")),
            ]
        };

        // Key difference from other tests: MessageId is NOT set on the assistant message
        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]), // Note: No MessageId set - this is the bug trigger
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        // Both FCCs should be in a SINGLE assistant message, not split across multiple messages
        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task ApprovedApprovalResponsesFromSeparateFCCMessagesAreExecutedAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp2" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
            ]),
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]) { MessageId = "resp2" },
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]) { MessageId = "resp2" },
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task RejectedApprovalResponsesAreFailedAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", false, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", false, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected."),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected.")
            ]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected."),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected.")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task MixedApprovedAndRejectedApprovalResponsesAreExecutedAndFailedAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", false, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Tool call invocation rejected.")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> nonStreamingOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Tool call invocation rejected.")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> streamingOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected."),
                new FunctionResultContent("callId2", result: "Result 2: 42")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, nonStreamingOutput, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, streamingOutput, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task RejectedApprovalResponsesWithCustomReasonAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", false, new FunctionCallContent("callId1", "Func1"))
                {
                    Reason = "User denied permission for this operation"
                },
                new FunctionApprovalResponseContent("callId2", false, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
                {
                    Reason = "Function Func2 is not allowed at this time"
                }
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected. User denied permission for this operation"),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected. Function Func2 is not allowed at this time")
            ]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected. User denied permission for this operation"),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected. Function Func2 is not allowed at this time")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task MixedApprovalResponsesWithCustomAndDefaultReasonsAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
                AIFunctionFactory.Create((string s) => $"Result 3: {s}", "Func3"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })),
                new FunctionApprovalRequestContent("callId3", new FunctionCallContent("callId3", "Func3", arguments: new Dictionary<string, object?> { { "s", "test" } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", false, new FunctionCallContent("callId1", "Func1")) { Reason = "Custom rejection for Func1" },
                new FunctionApprovalResponseContent("callId2", false, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })),
                new FunctionApprovalResponseContent("callId3", true, new FunctionCallContent("callId3", "Func3", arguments: new Dictionary<string, object?> { { "s", "test" } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "Func1"),
                new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }),
                new FunctionCallContent("callId3", "Func3", arguments: new Dictionary<string, object?> { { "s", "test" } })
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected. Custom rejection for Func1"),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected.")
            ]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Result 3: test")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> nonStreamingOutput =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "Func1"),
                new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }),
                new FunctionCallContent("callId3", "Func3", arguments: new Dictionary<string, object?> { { "s", "test" } })
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected. Custom rejection for Func1"),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected.")
            ]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Result 3: test")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> streamingOutput =
        [
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("callId1", "Func1"),
                new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }),
                new FunctionCallContent("callId3", "Func3", arguments: new Dictionary<string, object?> { { "s", "test" } })
            ]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected. Custom rejection for Func1"),
                new FunctionResultContent("callId2", result: "Tool call invocation rejected."),
                new FunctionResultContent("callId3", result: "Result 3: test")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, nonStreamingOutput, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, streamingOutput, expectedDownstreamClientInput);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RejectedApprovalResponsesWithEmptyOrWhitespaceReasonUsesDefaultMessageAsync(string? reason)
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", false, new FunctionCallContent("callId1", "Func1"))
                {
                    Reason = reason
                },
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected.")
            ]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]),
            new ChatMessage(ChatRole.Tool,
            [
                new FunctionResultContent("callId1", result: "Tool call invocation rejected.")
            ]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task ApprovedInputsAreExecutedAndFunctionResultsAreConvertedAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2")),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 3 } })]),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 3 } }))
            ]),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task AlreadyExecutedApprovalsAreIgnoredAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2")),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalRequestContent("callId2", new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]) { MessageId = "resp1" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId3", new FunctionCallContent("callId3", "Func1")),
            ]) { MessageId = "resp2" },
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId3", true, new FunctionCallContent("callId3", "Func1")),
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Result 1")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "World"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId3", "Func1")]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId3", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "World"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    /// <summary>
    /// This verifies the following scenario:
    /// 1. We are streaming (also including non-streaming in the test for completeness).
    /// 2. There is one function that requires approval and one that does not.
    /// 3. We only get back FCC for the function that does not require approval.
    /// 4. This means that once we receive this FCC, we need to buffer all updates until the end, because we might receive more FCCs and some may require approval.
    /// 5. We then need to verify that we will still stream all updates once we reach the end, including the buffered FCC.
    /// </summary>
    [Fact]
    public async Task MixedApprovalRequiredToolsWithNonApprovalRequiringFunctionCallAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
        ];

        Func<Queue<List<ChatMessage>>> expectedDownstreamClientInput = () => new Queue<List<ChatMessage>>(
        [
            new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "hello"),
            },
            new List<ChatMessage>
            {
                new ChatMessage(ChatRole.User, "hello"),
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
                new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")])
            }
        ]);

        Func<Queue<List<ChatMessage>>> downstreamClientOutput = () => new Queue<List<ChatMessage>>(
        [
            new List<ChatMessage>
            {
                new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            },
            new List<ChatMessage>
            {
                new ChatMessage(ChatRole.Assistant, "World again"),
            }
        ]);

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "World again"),
        ];

        await InvokeAndAssertMultiRoundAsync(options, input, downstreamClientOutput(), output, expectedDownstreamClientInput());

        await InvokeAndAssertStreamingMultiRoundAsync(options, input, downstreamClientOutput(), output, expectedDownstreamClientInput());
    }

    [Fact]
    public async Task ApprovalRequestWithoutApprovalResponseThrowsAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("callId1", new FunctionCallContent("callId1", "Func1")),
            ]) { MessageId = "resp1" },
        ];

        var invokeException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await InvokeAndAssertAsync(options, input, [], [], []));
        Assert.Equal("FunctionApprovalRequestContent found with FunctionCall.CallId(s) 'callId1' that have no matching FunctionApprovalResponseContent.", invokeException.Message);

        var invokeStreamingException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await InvokeAndAssertStreamingAsync(options, input, [], [], []));
        Assert.Equal("FunctionApprovalRequestContent found with FunctionCall.CallId(s) 'callId1' that have no matching FunctionApprovalResponseContent.", invokeStreamingException.Message);
    }

    [Fact]
    public async Task ApprovedApprovalResponsesWithoutApprovalRequestAreExecutedAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    [Fact]
    public async Task FunctionCallContentIsNotPassedToDownstreamServiceWithServiceThreadsAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ],
            ConversationId = "test-conversation",
        };

        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("callId1", true, new FunctionCallContent("callId1", "Func1")),
                new FunctionApprovalResponseContent("callId2", true, new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } }))
            ]),
        ];

        List<ChatMessage> expectedDownstreamClientInput =
        [
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        List<ChatMessage> output =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1"), new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })]),
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("callId1", result: "Result 1"), new FunctionResultContent("callId2", result: "Result 2: 42")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        await InvokeAndAssertAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);

        await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, output, expectedDownstreamClientInput);
    }

    /// <summary>
    /// Since we do not have a way of supporting both functions that require approval and those that do not
    /// in one invocation, we always require all function calls to be approved if any require approval.
    /// If we are therefore unsure as to whether we will encounter a function call that requires approval,
    /// we have to wait until we find one before yielding any function call content.
    /// If we don't have any function calls that require approval at all though, we can just yield all content normally
    /// since this issue won't apply.
    /// </summary>
    [Fact]
    public async Task FunctionCallContentIsYieldedImmediatelyIfNoApprovalRequiredWhenStreamingAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2"),
            ]
        };

        List<ChatMessage> input = [new ChatMessage(ChatRole.User, "hello")];

        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = b => b.Use(s => new FunctionInvokingChatClient(s));
        using CancellationTokenSource cts = new();

        var updateYieldCount = 0;

        async IAsyncEnumerable<ChatResponseUpdate> YieldInnerClientUpdates(
            IEnumerable<ChatMessage> contents, ChatOptions? actualOptions, [EnumeratorCancellation] CancellationToken actualCancellationToken)
        {
            Assert.Equal(cts.Token, actualCancellationToken);
            await Task.Yield();
            var messageId = Guid.NewGuid().ToString("N");

            updateYieldCount++;
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]) { MessageId = messageId };
            updateYieldCount++;
            yield return
                new ChatResponseUpdate(
                    ChatRole.Assistant,
                    [
                        new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })
                    ])
                { MessageId = messageId };
        }

        using var innerClient = new TestChatClient { GetStreamingResponseAsyncCallback = YieldInnerClientUpdates };
        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build();

        var updates = service.GetStreamingResponseAsync(new EnumeratedOnceEnumerable<ChatMessage>(input), options, cts.Token);

        var updateCount = 0;
        await foreach (var update in updates)
        {
            if (updateCount < 2)
            {
                var functionCall = update.Contents.OfType<FunctionCallContent>().First();
                if (functionCall.CallId == "callId1")
                {
                    Assert.Equal("Func1", functionCall.Name);
                    Assert.Equal(1, updateYieldCount);
                }
                else if (functionCall.CallId == "callId2")
                {
                    Assert.Equal("Func2", functionCall.Name);
                    Assert.Equal(2, updateYieldCount);
                }
            }

            updateCount++;
        }
    }

    /// <summary>
    /// Since we do not have a way of supporting both functions that require approval and those that do not
    /// in one invocation, we always require all function calls to be approved if any require approval.
    /// If we are therefore unsure as to whether we will encounter a function call that requires approval,
    /// we have to wait until we find one before yielding any function call content.
    /// We can however, yield any other content until we encounter the first function call.
    /// </summary>
    [Fact]
    public async Task FunctionCalsAreBufferedUntilApprovalRequirementEncounteredWhenStreamingAsync()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                AIFunctionFactory.Create(() => "Result 1", "Func1"),
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create((int i) => $"Result 2: {i}", "Func2")),
                AIFunctionFactory.Create(() => "Result 3", "Func3"),
            ]
        };

        List<ChatMessage> input = [new ChatMessage(ChatRole.User, "hello")];

        Func<ChatClientBuilder, ChatClientBuilder> configurePipeline = b => b.Use(s => new FunctionInvokingChatClient(s));
        using CancellationTokenSource cts = new();

        var updateYieldCount = 0;

        async IAsyncEnumerable<ChatResponseUpdate> YieldInnerClientUpdates(
            IEnumerable<ChatMessage> contents, ChatOptions? actualOptions, [EnumeratorCancellation] CancellationToken actualCancellationToken)
        {
            Assert.Equal(cts.Token, actualCancellationToken);
            await Task.Yield();
            var messageId = Guid.NewGuid().ToString("N");

            updateYieldCount++;
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Text 1")]) { MessageId = messageId };
            updateYieldCount++;
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Text 2")]) { MessageId = messageId };
            updateYieldCount++;
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func1")]) { MessageId = messageId };
            updateYieldCount++;
            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                [
                    new FunctionCallContent("callId2", "Func2", arguments: new Dictionary<string, object?> { { "i", 42 } })
                ])
            { MessageId = messageId };
            updateYieldCount++;
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("callId1", "Func3")]) { MessageId = messageId };
        }

        using var innerClient = new TestChatClient { GetStreamingResponseAsyncCallback = YieldInnerClientUpdates };
        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build();

        var updates = service.GetStreamingResponseAsync(new EnumeratedOnceEnumerable<ChatMessage>(input), options, cts.Token);

        var updateCount = 0;
        await foreach (var update in updates)
        {
            switch (updateCount)
            {
                case 0:
                    Assert.Equal("Text 1", update.Contents.OfType<TextContent>().First().Text);

                    // First content should be yielded immedately, since we don't have any function calls yet.
                    Assert.Equal(1, updateYieldCount);
                    break;
                case 1:
                    Assert.Equal("Text 2", update.Contents.OfType<TextContent>().First().Text);

                    // Second content should be yielded immedately, since we don't have any function calls yet.
                    Assert.Equal(2, updateYieldCount);
                    break;
                case 2:
                    var approvalRequest1 = update.Contents.OfType<FunctionApprovalRequestContent>().First();
                    Assert.Equal("callId1", approvalRequest1.FunctionCall.CallId);
                    Assert.Equal("Func1", approvalRequest1.FunctionCall.Name);

                    // Third content should have been buffered, since we have not yet encountered a function call that requires approval.
                    Assert.Equal(4, updateYieldCount);
                    break;
                case 3:
                    var approvalRequest2 = update.Contents.OfType<FunctionApprovalRequestContent>().First();
                    Assert.Equal("callId2", approvalRequest2.FunctionCall.CallId);
                    Assert.Equal("Func2", approvalRequest2.FunctionCall.Name);

                    // Fourth content can be yielded immediately, since it is the first function call that requires approval.
                    Assert.Equal(4, updateYieldCount);
                    break;
                case 4:
                    var approvalRequest3 = update.Contents.OfType<FunctionApprovalRequestContent>().First();
                    Assert.Equal("callId1", approvalRequest3.FunctionCall.CallId);
                    Assert.Equal("Func3", approvalRequest3.FunctionCall.Name);

                    // Fifth content can be yielded immediately, since we previously encountered a function call that requires approval.
                    Assert.Equal(5, updateYieldCount);
                    break;
            }

            updateCount++;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FunctionCallsWithInformationalOnlyTrueAreNotReplacedWithApprovalsAsync(bool streaming)
    {
        var functionInvokedCount = 0;
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(
                    AIFunctionFactory.Create(() => { functionInvokedCount++; return "Result 1"; }, "Func1")),
            ]
        };

        List<ChatMessage> input = [new ChatMessage(ChatRole.User, "hello")];

        // FunctionCallContent with InformationalOnly = true should pass through unchanged
        var alreadyProcessedFunctionCall = new FunctionCallContent("callId1", "Func1") { InformationalOnly = true };
        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, [alreadyProcessedFunctionCall]),
        ];

        // Expected output should contain the same FunctionCallContent, not a FunctionApprovalRequestContent
        List<ChatMessage> expectedOutput =
        [
            new ChatMessage(ChatRole.Assistant, [alreadyProcessedFunctionCall]),
        ];

        if (streaming)
        {
            await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, expectedOutput);
        }
        else
        {
            await InvokeAndAssertAsync(options, input, downstreamClientOutput, expectedOutput);
        }

        // The function should NOT have been invoked since InformationalOnly was true
        Assert.Equal(0, functionInvokedCount);
    }

    [Fact]
    public async Task ApprovalResponsePreservesOriginalRequestMessageMetadata()
    {
        var options = new ChatOptions
        {
            Tools =
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => "Result 1", "Func1")),
            ]
        };

        const string OriginalMessageId = "original-message-id";

        // Create input with approval request containing a known MessageId on the containing message
        List<ChatMessage> input =
        [
            new ChatMessage(ChatRole.User, "hello"),
            new ChatMessage(ChatRole.Assistant,
            [
                new FunctionApprovalRequestContent("approval-request-id", new FunctionCallContent("function-call-id", "Func1"))
            ]) { MessageId = OriginalMessageId }, // This MessageId should be preserved
            new ChatMessage(ChatRole.User,
            [
                new FunctionApprovalResponseContent("approval-request-id", true, new FunctionCallContent("function-call-id", "Func1"))
            ]),
        ];

        List<ChatMessage> downstreamClientOutput =
        [
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        // The reconstructed function call message should preserve the original MessageId
        List<ChatMessage> expectedOutput =
        [
            new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("function-call-id", "Func1")]) { MessageId = OriginalMessageId },
            new ChatMessage(ChatRole.Tool, [new FunctionResultContent("function-call-id", result: "Result 1")]),
            new ChatMessage(ChatRole.Assistant, "world"),
        ];

        var actualOutput = await InvokeAndAssertAsync(options, input, downstreamClientOutput, expectedOutput);

        // Verify that the reconstructed function call message has the original MessageId, not a synthetic one
        Assert.Equal(OriginalMessageId, actualOutput[0].MessageId);

        actualOutput = await InvokeAndAssertStreamingAsync(options, input, downstreamClientOutput, expectedOutput);
        Assert.Equal(OriginalMessageId, actualOutput[0].MessageId);
    }

    private static Task<List<ChatMessage>> InvokeAndAssertAsync(
        ChatOptions? options,
        List<ChatMessage> input,
        List<ChatMessage> downstreamClientOutput,
        List<ChatMessage> expectedOutput,
        List<ChatMessage>? expectedDownstreamClientInput = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        AITool[]? additionalTools = null)
        => InvokeAndAssertMultiRoundAsync(
            options,
            input,
            new Queue<List<ChatMessage>>(new[] { downstreamClientOutput }),
            expectedOutput,
            expectedDownstreamClientInput is null ? null : new Queue<List<ChatMessage>>(new[] { expectedDownstreamClientInput }),
            configurePipeline,
            additionalTools);

    private static async Task<List<ChatMessage>> InvokeAndAssertMultiRoundAsync(
        ChatOptions? options,
        List<ChatMessage> input,
        Queue<List<ChatMessage>> downstreamClientOutput,
        List<ChatMessage> expectedOutput,
        Queue<List<ChatMessage>>? expectedDownstreamClientInput = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        AITool[]? additionalTools = null)
    {
        Assert.NotEmpty(input);

        configurePipeline ??= b => b.Use(s => new FunctionInvokingChatClient(s) { AdditionalTools = additionalTools });

        using CancellationTokenSource cts = new();
        long expectedTotalTokenCounts = 0;

        using var innerClient = new TestChatClient
        {
            GetResponseAsyncCallback = async (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Equal(cts.Token, actualCancellationToken);
                if (expectedDownstreamClientInput is not null)
                {
                    AssertExtensions.EqualMessageLists(expectedDownstreamClientInput.Dequeue(), contents.ToList());
                }

                await Task.Yield();

                var usage = CreateRandomUsage();
                expectedTotalTokenCounts += usage.InputTokenCount!.Value;

                var output = downstreamClientOutput.Dequeue();
                output.ForEach(m => m.MessageId = Guid.NewGuid().ToString("N"));
                return new ChatResponse(output) { Usage = usage };
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build();

        var result = await service.GetResponseAsync(new EnumeratedOnceEnumerable<ChatMessage>(input), options, cts.Token);
        Assert.NotNull(result);

        var actualOutput = result.Messages as List<ChatMessage> ?? result.Messages.ToList();
        AssertExtensions.EqualMessageLists(expectedOutput, actualOutput);

        // Usage should be aggregated over all responses, including AdditionalUsage
        var actualUsage = result.Usage!;
        Assert.Equal(expectedTotalTokenCounts, actualUsage.InputTokenCount);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.OutputTokenCount);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.TotalTokenCount);
        Assert.Equal(2, actualUsage.AdditionalCounts!.Count);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.AdditionalCounts["firstValue"]);
        Assert.Equal(expectedTotalTokenCounts, actualUsage.AdditionalCounts["secondValue"]);

        return actualOutput;
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

    private static Task<List<ChatMessage>> InvokeAndAssertStreamingAsync(
        ChatOptions? options,
        List<ChatMessage> input,
        List<ChatMessage> downstreamClientOutput,
        List<ChatMessage> expectedOutput,
        List<ChatMessage>? expectedDownstreamClientInput = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        AITool[]? additionalTools = null)
        => InvokeAndAssertStreamingMultiRoundAsync(
            options,
            input,
            new Queue<List<ChatMessage>>(new[] { downstreamClientOutput }),
            expectedOutput,
            expectedDownstreamClientInput is null ? null : new Queue<List<ChatMessage>>(new[] { expectedDownstreamClientInput }),
            configurePipeline,
            additionalTools);

    private static async Task<List<ChatMessage>> InvokeAndAssertStreamingMultiRoundAsync(
        ChatOptions? options,
        List<ChatMessage> input,
        Queue<List<ChatMessage>> downstreamClientOutput,
        List<ChatMessage> expectedOutput,
        Queue<List<ChatMessage>>? expectedDownstreamClientInput = null,
        Func<ChatClientBuilder, ChatClientBuilder>? configurePipeline = null,
        AITool[]? additionalTools = null)
    {
        Assert.NotEmpty(input);

        configurePipeline ??= b => b.Use(s => new FunctionInvokingChatClient(s) { AdditionalTools = additionalTools });

        using CancellationTokenSource cts = new();

        using var innerClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (contents, actualOptions, actualCancellationToken) =>
            {
                Assert.Equal(cts.Token, actualCancellationToken);
                if (expectedDownstreamClientInput is not null)
                {
                    AssertExtensions.EqualMessageLists(expectedDownstreamClientInput.Dequeue(), contents.ToList());
                }

                var output = downstreamClientOutput.Dequeue();
                output.ForEach(m => m.MessageId = Guid.NewGuid().ToString("N"));
                return YieldAsync(new ChatResponse(output).ToChatResponseUpdates());
            }
        };

        IChatClient service = configurePipeline(innerClient.AsBuilder()).Build();

        var result = await service.GetStreamingResponseAsync(new EnumeratedOnceEnumerable<ChatMessage>(input), options, cts.Token).ToChatResponseAsync();
        Assert.NotNull(result);

        var actualOutput = result.Messages as List<ChatMessage> ?? result.Messages.ToList();

        expectedOutput ??= input;
        AssertExtensions.EqualMessageLists(expectedOutput, actualOutput);

        return actualOutput;
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
