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
        ChatMessage message = new(ChatRole.Assistant,
        [
            new FunctionCallContent("call1", "function1", new Dictionary<string, object?> { { "param1", 123 } }),
            new McpServerToolCallContent("call2", "myTool", "myServer"),
            new CodeInterpreterToolCallContent("call3"),
            new ImageGenerationToolCallContent("call4"),
        ]);

        // Verify each element roundtrips individually
        foreach (var content in message.Contents)
        {
            var serialized = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
            var deserialized = JsonSerializer.Deserialize<ToolCallContent>(serialized, AIJsonUtilities.DefaultOptions);
            Assert.NotNull(deserialized);
            Assert.Equal(content.GetType(), deserialized.GetType());
        }

        // Verify the message roundtrips - can't use Array because that's not included as 
        // JsonSerializable in AIJsonUtilities and we can't use TestJsonSerializerContext here 
        // because it doesn't include the experimental types.
        var serializedMessage = JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions);
        ChatMessage? deserialized2 = JsonSerializer.Deserialize<ChatMessage>(serializedMessage, AIJsonUtilities.DefaultOptions);
        Assert.NotNull(deserialized2);

        Assert.Equal(message.Role, deserialized2.Role);
        Assert.Equal(message.Contents.Count, deserialized2.Contents.Count);
        for (int i = 0; i < message.Contents.Count; i++)
        {
            Assert.NotNull(deserialized2.Contents[i]);
            Assert.Equal(message.Contents[i].GetType(), deserialized2.Contents[i].GetType());
        }
    }
}
