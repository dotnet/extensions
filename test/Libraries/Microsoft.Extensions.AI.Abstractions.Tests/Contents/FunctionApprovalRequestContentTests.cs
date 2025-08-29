// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class FunctionApprovalRequestContentTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_SetsIdAndFunctionCall(string id)
    {
        var functionCall = new FunctionCallContent("FCC1", "TestFunction");
        var content = new FunctionApprovalRequestContent(id, functionCall);

        Assert.Equal(id, content.Id);
        Assert.Same(functionCall, content.FunctionCall);
    }

    [Fact]
    public void Constructor_ThrowsOnNullFunctionCall()
    {
        Assert.ThrowsAny<ArgumentNullException>(() => new FunctionApprovalRequestContent("id", null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhitespaceId(string? id)
    {
        var functionCall = new FunctionCallContent("FCC1", "TestFunction");
        Assert.ThrowsAny<ArgumentException>(() => new FunctionApprovalRequestContent(id!, functionCall));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateResponse_ReturnsExpectedResponse(bool approved)
    {
        var id = "req-1";
        var functionCall = new FunctionCallContent("FCC1", "TestFunction");
        var content = new FunctionApprovalRequestContent(id, functionCall);

        var response = content.CreateResponse(approved);

        Assert.Equal(id, response.Id);
        Assert.Equal(approved, response.Approved);
        Assert.Same(functionCall, response.FunctionCall);
    }
}
