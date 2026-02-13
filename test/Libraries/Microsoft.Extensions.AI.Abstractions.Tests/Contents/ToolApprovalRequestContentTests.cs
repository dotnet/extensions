// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class ToolApprovalRequestContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");
        McpServerToolCallContent mcpCall = new("MCC1", "TestTool", "TestServer");

        // FunctionCallContent overload
        Assert.Throws<ArgumentNullException>("requestId", () => new ToolApprovalRequestContent(null!, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalRequestContent("", functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalRequestContent("\r\t\n ", functionCall));
        Assert.Throws<ArgumentNullException>("functionCall", () => new ToolApprovalRequestContent("id", (FunctionCallContent)null!));

        // McpServerToolCallContent overload
        Assert.Throws<ArgumentNullException>("requestId", () => new ToolApprovalRequestContent(null!, mcpCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalRequestContent("", mcpCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalRequestContent("\r\t\n ", mcpCall));
        Assert.Throws<ArgumentNullException>("mcpServerToolCall", () => new ToolApprovalRequestContent("id", (McpServerToolCallContent)null!));

        // ToolCallContent (JsonConstructor) overload
        Assert.Throws<ArgumentNullException>("toolCall", () => new ToolApprovalRequestContent("id", (ToolCallContent)null!));
        Assert.Throws<ArgumentException>("toolCall", () => new ToolApprovalRequestContent("id", new CodeInterpreterToolCallContent("call1")));
        Assert.Throws<ArgumentException>("toolCall", () => new ToolApprovalRequestContent("id", new ImageGenerationToolCallContent("call1")));
    }

    public static TheoryData<ToolCallContent> ToolCallContentInstances => new()
    {
        new FunctionCallContent("FCC1", "TestFunction", new Dictionary<string, object?> { { "param1", 123 } }),
        new McpServerToolCallContent("MCC1", "TestTool", "TestServer") { Arguments = new Dictionary<string, object?> { { "arg1", "value1" } } },
    };

    [Theory]
    [MemberData(nameof(ToolCallContentInstances))]
    public void Constructor_Roundtrips(ToolCallContent toolCall)
    {
        string id = "req-1";
        ToolApprovalRequestContent content = new(id, toolCall);

        Assert.Same(id, content.RequestId);
        Assert.Same(toolCall, content.ToolCall);
    }

    [Theory]
    [MemberData(nameof(ToolCallContentInstances))]
    public void CreateResponse_ReturnsExpectedResponse(ToolCallContent toolCall)
    {
        string id = "req-1";
        ToolApprovalRequestContent content = new(id, toolCall);

        var response = content.CreateResponse(approved: true);

        Assert.NotNull(response);
        Assert.Same(id, response.RequestId);
        Assert.True(response.Approved);
        Assert.Same(toolCall, response.ToolCall);
        Assert.Null(response.Reason);
    }

    [Theory]
    [InlineData(true, "Approved for testing")]
    [InlineData(false, "Rejected due to security concerns")]
    [InlineData(true, null)]
    [InlineData(false, null)]
    public void CreateResponse_WithReason_ReturnsExpectedResponse(bool approved, string? reason)
    {
        string id = "req-1";
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        ToolApprovalRequestContent content = new(id, functionCall);

        var response = content.CreateResponse(approved, reason);

        Assert.NotNull(response);
        Assert.Same(id, response.RequestId);
        Assert.Equal(approved, response.Approved);
        Assert.Same(functionCall, response.ToolCall);
        Assert.Equal(reason, response.Reason);
    }

    [Theory]
    [MemberData(nameof(ToolCallContentInstances))]
    public void Serialization_Roundtrips(ToolCallContent toolCall)
    {
        var content = new ToolApprovalRequestContent("request123", toolCall);

        AssertSerializationRoundtrips<ToolApprovalRequestContent>(content);
        AssertSerializationRoundtrips<InputRequestContent>(content);
        AssertSerializationRoundtrips<AIContent>(content);

        static void AssertSerializationRoundtrips<T>(ToolApprovalRequestContent content)
            where T : AIContent
        {
            T contentAsT = (T)(object)content;
            string json = JsonSerializer.Serialize(contentAsT, AIJsonUtilities.DefaultOptions);
            T? deserialized = JsonSerializer.Deserialize<T>(json, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            var deserializedContent = Assert.IsType<ToolApprovalRequestContent>(deserialized);
            Assert.Equal(content.RequestId, deserializedContent.RequestId);
            Assert.NotNull(deserializedContent.ToolCall);
            Assert.IsType(content.ToolCall.GetType(), deserializedContent.ToolCall);
            Assert.Equal(content.ToolCall.CallId, deserializedContent.ToolCall.CallId);
        }
    }
}
