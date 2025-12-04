// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class FunctionApprovalResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");

        Assert.Throws<ArgumentNullException>("requestId", () => new FunctionApprovalResponseContent(null!, true, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new FunctionApprovalResponseContent("", true, functionCall));
        Assert.Throws<ArgumentException>("requestId", () => new FunctionApprovalResponseContent("\r\t\n ", true, functionCall));

        Assert.Throws<ArgumentNullException>("functionCall", () => new FunctionApprovalResponseContent("requestId", true, null!));
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("123", false)]
    [InlineData("!@#", true)]
    public void Constructor_Roundtrips(string requestId, bool approved)
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");
        FunctionApprovalResponseContent content = new(requestId, approved, functionCall);

        Assert.Same(requestId, content.RequestId);
        Assert.Equal(approved, content.Approved);
        Assert.Same(functionCall, content.FunctionCall);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new FunctionApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName"));

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<FunctionApprovalResponseContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.RequestId, deserializedContent.RequestId);
        Assert.Equal(content.Approved, deserializedContent.Approved);
        Assert.NotNull(deserializedContent.FunctionCall);
        Assert.Equal(content.FunctionCall.CallId, deserializedContent.FunctionCall.CallId);
        Assert.Equal(content.FunctionCall.Name, deserializedContent.FunctionCall.Name);
    }
}
