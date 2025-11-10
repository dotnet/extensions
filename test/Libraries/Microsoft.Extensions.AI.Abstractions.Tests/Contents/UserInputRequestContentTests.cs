// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class UserInputRequestContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new TestUserInputRequestContent(null!));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputRequestContent(""));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputRequestContent("\r\t\n "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        TestUserInputRequestContent content = new(id);

        Assert.Equal(id, content.Id);
    }

    [Fact]
    public void Serialization_DerivedTypes_Roundtrips()
    {
        UserInputRequestContent content = new FunctionApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } }));
        var serializedContent = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<UserInputRequestContent>(serializedContent, AIJsonUtilities.DefaultOptions);
        Assert.NotNull(deserializedContent);
        Assert.Equal(content.GetType(), deserializedContent.GetType());

        UserInputRequestContent[] contents =
        [
            new FunctionApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } })),
            new McpServerToolApprovalRequestContent("request123", new McpServerToolCallContent("call123", "myTool", "myServer")),
        ];

        var serializedContents = JsonSerializer.Serialize(contents, TestJsonSerializerContext.Default.UserInputRequestContentArray);
        var deserializedContents = JsonSerializer.Deserialize(serializedContents, TestJsonSerializerContext.Default.UserInputRequestContentArray);
        Assert.NotNull(deserializedContents);

        Assert.Equal(contents.Count(), deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(contents.ElementAt(i));
            Assert.Equal(contents.ElementAt(i).GetType(), deserializedContents[i].GetType());
        }
    }

    private sealed class TestUserInputRequestContent : UserInputRequestContent
    {
        public TestUserInputRequestContent(string id)
            : base(id)
        {
        }
    }
}
