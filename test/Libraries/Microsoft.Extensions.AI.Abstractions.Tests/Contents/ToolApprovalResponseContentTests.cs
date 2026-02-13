// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class ToolApprovalResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");
        McpServerToolCallContent mcpCall = new("MCC1", "TestTool", "TestServer");

        // FunctionCallContent overload
        Assert.Throws<ArgumentNullException>("requestId", () => new ToolApprovalResponseContent(null!, true, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalResponseContent("", true, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalResponseContent("\r\t\n ", true, functionCall));
        Assert.Throws<ArgumentNullException>("functionCall", () => new ToolApprovalResponseContent("id", true, (FunctionCallContent)null!));

        // McpServerToolCallContent overload
        Assert.Throws<ArgumentNullException>("requestId", () => new ToolApprovalResponseContent(null!, true, mcpCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalResponseContent("", true, mcpCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalResponseContent("\r\t\n ", true, mcpCall));
        Assert.Throws<ArgumentNullException>("mcpServerToolCall", () => new ToolApprovalResponseContent("id", true, (McpServerToolCallContent)null!));

        // ToolCallContent (JsonConstructor) overload
        Assert.Throws<ArgumentNullException>("toolCall", () => new ToolApprovalResponseContent("id", true, (ToolCallContent)null!));
        Assert.Throws<ArgumentException>("toolCall", () => new ToolApprovalResponseContent("id", true, new CodeInterpreterToolCallContent("call1")));
        Assert.Throws<ArgumentException>("toolCall", () => new ToolApprovalResponseContent("id", true, new ImageGenerationToolCallContent("call1")));
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
        ToolApprovalResponseContent content = new("req-1", true, toolCall);

        Assert.Equal("req-1", content.RequestId);
        Assert.True(content.Approved);
        Assert.Same(toolCall, content.ToolCall);

        content = new("req-2", false, toolCall);

        Assert.Equal("req-2", content.RequestId);
        Assert.False(content.Approved);
        Assert.Same(toolCall, content.ToolCall);
    }

    [Theory]
    [MemberData(nameof(ToolCallContentInstances))]
    public void Serialization_Roundtrips(ToolCallContent toolCall)
    {
        var content = new ToolApprovalResponseContent("request123", true, toolCall)
        {
            Reason = "Approved for testing"
        };

        AssertSerializationRoundtrips<ToolApprovalResponseContent>(content);
        AssertSerializationRoundtrips<InputResponseContent>(content);
        AssertSerializationRoundtrips<AIContent>(content);

        static void AssertSerializationRoundtrips<T>(ToolApprovalResponseContent content)
            where T : AIContent
        {
            T contentAsT = (T)(object)content;
            string json = JsonSerializer.Serialize(contentAsT, AIJsonUtilities.DefaultOptions);
            T? deserialized = JsonSerializer.Deserialize<T>(json, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            var deserializedContent = Assert.IsType<ToolApprovalResponseContent>(deserialized);
            Assert.Equal(content.RequestId, deserializedContent.RequestId);
            Assert.Equal(content.Approved, deserializedContent.Approved);
            Assert.Equal(content.Reason, deserializedContent.Reason);
            Assert.NotNull(deserializedContent.ToolCall);
            Assert.IsType(content.ToolCall.GetType(), deserializedContent.ToolCall);
            Assert.Equal(content.ToolCall.CallId, deserializedContent.ToolCall.CallId);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Custom rejection reason")]
    public void Serialization_WithReason_Roundtrips(string? reason)
    {
        var content = new ToolApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName"))
        {
            Reason = reason
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<ToolApprovalResponseContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.RequestId, deserializedContent.RequestId);
        Assert.Equal(content.Approved, deserializedContent.Approved);
        Assert.Equal(content.Reason, deserializedContent.Reason);
        Assert.NotNull(deserializedContent.ToolCall);
        var functionCall = Assert.IsType<FunctionCallContent>(deserializedContent.ToolCall);
        Assert.Equal(content.ToolCall.CallId, functionCall.CallId);
        Assert.Equal(((FunctionCallContent)content.ToolCall).Name, functionCall.Name);
    }
}
