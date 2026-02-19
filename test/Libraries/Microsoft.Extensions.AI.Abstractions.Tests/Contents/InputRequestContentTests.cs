// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class InputRequestContentTests
{
    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>("requestId", () => new TestInputRequestContent(null!));
        Assert.Throws<ArgumentException>("requestId", () => new TestInputRequestContent(""));
        Assert.Throws<ArgumentException>("requestId", () => new TestInputRequestContent("\r\t\n "));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("!@#")]
    public void Constructor_Roundtrips(string id)
    {
        TestInputRequestContent content = new(id);

        Assert.Equal(id, content.RequestId);
    }

    [Fact]
    public void Serialization_DerivedTypes_Roundtrips()
    {
        InputRequestContent[] contents =
        [
            new FunctionApprovalRequestContent("request123", new FunctionCallContent("call123", "functionName", new Dictionary<string, object?> { { "param1", 123 } })),
            new FunctionApprovalRequestContent("request456", new McpServerToolCallContent("call456", "myTool", "myServer")),
        ];

        // Verify each element roundtrips individually
        foreach (var content in contents)
        {
            var serialized = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<InputRequestContent>(serialized, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(content.GetType(), deserialized.GetType());
        }

        // Verify the array roundtrips
        var serializedContents = JsonSerializer.Serialize(contents, TestJsonSerializerContext.Default.InputRequestContentArray);
        var deserializedContents = JsonSerializer.Deserialize(serializedContents, TestJsonSerializerContext.Default.InputRequestContentArray);
        Assert.NotNull(deserializedContents);
        Assert.Equal(contents.Length, deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(deserializedContents[i]);
            Assert.Equal(contents[i].GetType(), deserializedContents[i].GetType());
        }
    }

    private sealed class TestInputRequestContent : InputRequestContent
    {
        public TestInputRequestContent(string requestId)
            : base(requestId)
        {
        }
    }
}
