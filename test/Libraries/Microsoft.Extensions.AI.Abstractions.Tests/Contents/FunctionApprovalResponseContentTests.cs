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

        Assert.Throws<ArgumentNullException>("id", () => new FunctionApprovalResponseContent(null!, true, functionCall));
        Assert.Throws<ArgumentException>("id", () => new FunctionApprovalResponseContent("", true, functionCall));
        Assert.Throws<ArgumentException>("id", () => new FunctionApprovalResponseContent("\r\t\n ", true, functionCall));

        Assert.Throws<ArgumentNullException>("callContent", () => new FunctionApprovalResponseContent("id", true, null!));
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("123", false)]
    [InlineData("!@#", true)]
    public void Constructor_Roundtrips(string id, bool approved)
    {
        FunctionCallContent functionCall = new("FCC1", "TestFunction");
        FunctionApprovalResponseContent content = new(id, approved, functionCall);

        Assert.Same(id, content.Id);
        Assert.Equal(approved, content.Approved);
        Assert.Same(functionCall, content.CallContent);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new FunctionApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName"));

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<FunctionApprovalResponseContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.Id, deserializedContent.Id);
        Assert.Equal(content.Approved, deserializedContent.Approved);
        Assert.NotNull(deserializedContent.CallContent);

        var deserializedFunctionCall = Assert.IsType<FunctionCallContent>(deserializedContent.CallContent);
        var originalFunctionCall = (FunctionCallContent)content.CallContent;
        Assert.Equal(originalFunctionCall.CallId, deserializedFunctionCall.CallId);
        Assert.Equal(originalFunctionCall.Name, deserializedFunctionCall.Name);
    }
}
