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
        UserInputResponseContent[] contents =
        [
            new FunctionApprovalResponseContent("request123", true, new FunctionCallContent("call123", "functionName")),
            new FunctionApprovalResponseContent("request456", true, new McpServerToolCallContent("call456", "myTool", "myServer")),
        ];

        // Verify each element roundtrips individually
        foreach (var content in contents)
        {
            var serialized = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<UserInputResponseContent>(serialized, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(content.GetType(), deserialized.GetType());
        }

        // Verify the array roundtrips
        var serializedContents = JsonSerializer.Serialize(contents, TestJsonSerializerContext.Default.UserInputResponseContentArray);
        var deserializedContents = JsonSerializer.Deserialize(serializedContents, TestJsonSerializerContext.Default.UserInputResponseContentArray);
        Assert.NotNull(deserializedContents);
        Assert.Equal(contents.Length, deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(deserializedContents[i]);
            Assert.Equal(contents[i].GetType(), deserializedContents[i].GetType());
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
