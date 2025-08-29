// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class FunctionApprovalResponseContentTests
{
    [Theory]
    [InlineData("abc", true)]
    [InlineData("123", false)]
    [InlineData("!@#", true)]
    public void Constructor_SetsProperties(string id, bool approved)
    {
        var functionCall = new FunctionCallContent("FCC1", "TestFunction");
        var content = new FunctionApprovalResponseContent(id, approved, functionCall);

        Assert.Equal(id, content.Id);
        Assert.Equal(approved, content.Approved);
        Assert.Same(functionCall, content.FunctionCall);
    }

    [Fact]
    public void Constructor_ThrowsOnNullFunctionCall()
    {
        Assert.ThrowsAny<ArgumentNullException>(() => new FunctionApprovalResponseContent("id", true, null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsOnNullOrWhitespaceId(string? id)
    {
        var functionCall = new FunctionCallContent("FCC1", "TestFunction");
        Assert.ThrowsAny<ArgumentException>(() => new FunctionApprovalResponseContent(id!, true, functionCall));
    }
}
