// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class FunctionApprovalRequestContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        Assert.Throws<ArgumentNullException>("requestId", () => new FunctionApprovalRequestContent(null!, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new FunctionApprovalRequestContent("", functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new FunctionApprovalRequestContent("\r\t\n ", functionCall));

        Assert.Throws<ArgumentNullException>("functionCall", () => new FunctionApprovalRequestContent("id", null!));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        FunctionApprovalRequestContent content = new(id, functionCall);

        Assert.Same(id, content.RequestId);
        Assert.Same(functionCall, content.FunctionCall);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateResponse_ReturnsExpectedResponse(bool approved)
    {
        string id = "req-1";
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        FunctionApprovalRequestContent content = new(id, functionCall);

        var response = content.CreateResponse(approved);

        Assert.NotNull(response);
        Assert.Same(id, response.RequestId);
        Assert.Equal(approved, response.Approved);
        Assert.Same(functionCall, response.FunctionCall);
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

        FunctionApprovalRequestContent content = new(id, functionCall);

        var response = content.CreateResponse(approved, reason);

        Assert.NotNull(response);
        Assert.Same(id, response.RequestId);
        Assert.Equal(approved, response.Approved);
        Assert.Same(functionCall, response.FunctionCall);
        Assert.Equal(reason, response.Reason);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new FunctionApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } }));

        AssertSerializationRoundtrips<FunctionApprovalRequestContent>(content);
        AssertSerializationRoundtrips<InputRequestContent>(content);
        AssertSerializationRoundtrips<AIContent>(content);

        static void AssertSerializationRoundtrips<T>(FunctionApprovalRequestContent content)
            where T : AIContent
        {
            T contentAsT = (T)(object)content;
            string json = JsonSerializer.Serialize(contentAsT, AIJsonUtilities.DefaultOptions);
            T? deserialized = JsonSerializer.Deserialize<T>(json, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            var deserializedContent = Assert.IsType<FunctionApprovalRequestContent>(deserialized);
            Assert.Equal(content.RequestId, deserializedContent.RequestId);
            Assert.NotNull(deserializedContent.FunctionCall);
            Assert.Equal(content.FunctionCall.CallId, deserializedContent.FunctionCall.CallId);
            Assert.Equal(content.FunctionCall.Name, deserializedContent.FunctionCall.Name);
        }
    }
}
