// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class UserInputResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("requestId", () => new TestUserInputResponseContent(null!));
        Assert.Throws<ArgumentException>("requestId", () => new TestUserInputResponseContent(""));
        Assert.Throws<ArgumentException>("requestId", () => new TestUserInputResponseContent("\r\t\n "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string requestId)
    {
        TestUserInputResponseContent content = new(requestId);

        Assert.Equal(requestId, content.RequestId);
    }

    [Fact]
    public void Serialization_DerivedTypes_Roundtrips()
    {
        UserInputResponseContent content = new FunctionApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName"));
        var serializedContent = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<UserInputResponseContent>(serializedContent, AIJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedContent);
        Assert.Equal(content.GetType(), deserializedContent.GetType());

        UserInputResponseContent[] contents =
        [
            new FunctionApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName")),
            new McpServerToolApprovalResponseContent("request123", true),
        ];

        var serializedContents = JsonSerializer.Serialize(contents, TestJsonSerializerContext.Default.UserInputResponseContentArray);
        var deserializedContents = JsonSerializer.Deserialize(serializedContents, TestJsonSerializerContext.Default.UserInputResponseContentArray);
        Assert.NotNull(deserializedContents);

        Assert.Equal(contents.Length, deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(contents[i]);
            Assert.Equal(contents[i].GetType(), deserializedContents[i].GetType());
        }
    }

    private class TestUserInputResponseContent : UserInputResponseContent
    {
        public TestUserInputResponseContent(string requestId)
            : base(requestId)
        {
        }
    }
}
