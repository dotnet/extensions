// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class ToolApprovalResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        Assert.Throws<ArgumentNullException>("requestId", () => new ToolApprovalResponseContent(null!, true, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalResponseContent("", true, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalResponseContent("\r\t\n ", true, functionCall));

        Assert.Throws<ArgumentNullException>("functionCall", () => new ToolApprovalResponseContent("id", true, (FunctionCallContent)null!));
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("123", false)]
    [InlineData("!@#", true)]
    public void Constructor_Roundtrips(string id, bool approved)
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");
        ToolApprovalResponseContent content = new(id, approved, functionCall);

        Assert.Same(id, content.RequestId);
        Assert.Equal(approved, content.Approved);
        Assert.Same(functionCall, content.ToolCall);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new ToolApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName"))
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
            var functionCall = Assert.IsType<FunctionCallContent>(deserializedContent.ToolCall);
            Assert.Equal(content.ToolCall.CallId, functionCall.CallId);
            Assert.Equal(((FunctionCallContent)content.ToolCall).Name, functionCall.Name);
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
