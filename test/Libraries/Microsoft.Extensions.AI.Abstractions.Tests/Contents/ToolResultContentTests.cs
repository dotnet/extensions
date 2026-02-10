// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ToolResultContentTests
{
    [Fact]
    public void Serialization_DerivedTypes_Roundtrips()
    {
        ToolResultContent[] contents =
        [
            new FunctionResultContent("call1", "result1"),
            new McpServerToolResultContent("call2"),
        ];

        // Verify each element roundtrips individually
        foreach (var content in contents)
        {
            var serialized = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<ToolResultContent>(serialized, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(content.GetType(), deserialized.GetType());
        }

        // Verify the array roundtrips
        var serializedContents = JsonSerializer.Serialize(contents, TestJsonSerializerContext.Default.ToolResultContentArray);
        var deserializedContents = JsonSerializer.Deserialize<ToolResultContent[]>(serializedContents, TestJsonSerializerContext.Default.ToolResultContentArray);
        Assert.NotNull(deserializedContents);
        Assert.Equal(contents.Length, deserializedContents.Length);
        for (int i = 0; i < deserializedContents.Length; i++)
        {
            Assert.NotNull(deserializedContents[i]);
            Assert.Equal(contents[i].GetType(), deserializedContents[i].GetType());
        }
    }
}
