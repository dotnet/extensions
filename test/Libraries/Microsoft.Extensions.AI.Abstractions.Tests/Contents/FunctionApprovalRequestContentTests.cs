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

        Assert.Throws<ArgumentNullException>("id", () => new FunctionApprovalRequestContent(null!, functionCall));
        Assert.Throws<ArgumentException>("id", () => new FunctionApprovalRequestContent("", functionCall));
        Assert.Throws<ArgumentException>("id", () => new FunctionApprovalRequestContent("\r\t\n ", functionCall));

        Assert.Throws<ArgumentNullException>("callContent", () => new FunctionApprovalRequestContent("id", null!));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        FunctionApprovalRequestContent content = new(id, functionCall);

        Assert.Same(id, content.Id);
        Assert.Same(functionCall, content.CallContent);
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
        Assert.Same(id, response.Id);
        Assert.Equal(approved, response.Approved);
        Assert.Same(functionCall, response.CallContent);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new FunctionApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } }));

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<FunctionApprovalRequestContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.Id, deserializedContent.Id);
        Assert.NotNull(deserializedContent.CallContent);

        var deserializedFunctionCall = Assert.IsType<FunctionCallContent>(deserializedContent.CallContent);
        var originalFunctionCall = (FunctionCallContent)content.CallContent;
        Assert.Equal(originalFunctionCall.CallId, deserializedFunctionCall.CallId);
        Assert.Equal(originalFunctionCall.Name, deserializedFunctionCall.Name);
    }
}
