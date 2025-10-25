// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public class UserInputResponseContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("id", () => new TestUserInputResponseContent(null!));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputResponseContent(""));
        Assert.Throws<ArgumentException>("id", () => new TestUserInputResponseContent("\r\t\n "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        TestUserInputResponseContent content = new(id);

        Assert.Equal(id, content.Id);
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

        Assert.Equal(contents.Count(), deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(contents.ElementAt(i));
            Assert.Equal(contents.ElementAt(i).GetType(), deserializedContents[i].GetType());
        }
    }

    private class TestUserInputResponseContent : UserInputResponseContent
    {
        public TestUserInputResponseContent(string id)
            : base(id)
        {
        }
    }
}
