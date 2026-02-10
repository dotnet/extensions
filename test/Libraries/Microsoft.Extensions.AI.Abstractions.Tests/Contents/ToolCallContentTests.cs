// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ToolCallContentTests
{
    [Fact]
    public void Serialization_DerivedTypes_Roundtrips()
    {
        ToolCallContent[] contents =
        [
            new FunctionCallContent("call1", "function1", new Dictionary<string, object?> { { "param1", 123 } }),
            new McpServerToolCallContent("call2", "myTool", "myServer"),
        ];

        // Verify each element roundtrips individually
        foreach (var content in contents)
        {
            var serialized = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<ToolCallContent>(serialized, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(content.GetType(), deserialized.GetType());
        }

        // Verify the array roundtrips
        var serializedContents = JsonSerializer.Serialize(contents, TestJsonSerializerContext.Default.ToolCallContentArray);
        var deserializedContents = JsonSerializer.Deserialize<ToolCallContent[]>(serializedContents, TestJsonSerializerContext.Default.ToolCallContentArray);
        Assert.NotNull(deserializedContents);
        Assert.Equal(contents.Length, deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(deserializedContents[i]);
            Assert.Equal(contents[i].GetType(), deserializedContents[i].GetType());
        }
    }
}
