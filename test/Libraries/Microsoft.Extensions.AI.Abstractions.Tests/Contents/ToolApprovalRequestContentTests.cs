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

        Assert.Throws<ArgumentNullException>("requestId", () => new ToolApprovalRequestContent(null!, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalRequestContent("", functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new ToolApprovalRequestContent("\r\t\n ", functionCall));

        Assert.Throws<ArgumentNullException>("functionCall", () => new ToolApprovalRequestContent("id", (FunctionCallContent)null!));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        ToolApprovalRequestContent content = new(id, functionCall);

        Assert.Same(id, content.RequestId);
        Assert.Same(functionCall, content.ToolCall);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateResponse_ReturnsExpectedResponse(bool approved)
    {
        string id = "req-1";
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        ToolApprovalRequestContent content = new(id, functionCall);

        var response = content.CreateResponse(approved);

        Assert.NotNull(response);
        Assert.Same(id, response.RequestId);
        Assert.Equal(approved, response.Approved);
        Assert.Same(functionCall, response.ToolCall);
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

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new ToolApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } }));

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
            var functionCall = Assert.IsType<FunctionCallContent>(deserializedContent.ToolCall);
            Assert.Equal(content.ToolCall.CallId, functionCall.CallId);
            Assert.Equal(((FunctionCallContent)content.ToolCall).Name, functionCall.Name);
        }
    }
}
