// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatResponseFormatTests
{
    [Fact]
    public void Singletons_Idempotent()
    {
        Assert.Same(ChatResponseFormat.Text, ChatResponseFormat.Text);
        Assert.Same(ChatResponseFormat.Json, ChatResponseFormat.Json);
    }

    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentException>("schemaName", () => new ChatResponseFormatJson(null, "name"));
        Assert.Throws<ArgumentException>("schemaDescription", () => new ChatResponseFormatJson(null, null, "description"));
        Assert.Throws<ArgumentException>("schemaName", () => new ChatResponseFormatJson(null, "name", "description"));
    }

    [Fact]
    public void Constructor_PropsDefaulted()
    {
        ChatResponseFormatJson f = new(null);
        Assert.Null(f.Schema);
        Assert.Null(f.SchemaName);
        Assert.Null(f.SchemaDescription);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        ChatResponseFormatJson f = new("{}", "name", "description");
        Assert.Equal("{}", f.Schema);
        Assert.Equal("name", f.SchemaName);
        Assert.Equal("description", f.SchemaDescription);
    }

    [Fact]
    public void Equality_ComparersProduceExpectedResults()
    {
        Assert.True(ChatResponseFormat.Text == ChatResponseFormat.Text);
        Assert.True(ChatResponseFormat.Text.Equals(ChatResponseFormat.Text));
        Assert.Equal(ChatResponseFormat.Text.GetHashCode(), ChatResponseFormat.Text.GetHashCode());
        Assert.False(ChatResponseFormat.Text.Equals(ChatResponseFormat.Json));
        Assert.False(ChatResponseFormat.Text.Equals(new ChatResponseFormatJson(null)));
        Assert.False(ChatResponseFormat.Text.Equals(new ChatResponseFormatJson("{}")));

        Assert.True(ChatResponseFormat.Json == ChatResponseFormat.Json);
        Assert.True(ChatResponseFormat.Json.Equals(ChatResponseFormat.Json));
        Assert.False(ChatResponseFormat.Json.Equals(ChatResponseFormat.Text));
        Assert.False(ChatResponseFormat.Json.Equals(new ChatResponseFormatJson("{}")));

        Assert.True(ChatResponseFormat.Json.Equals(new ChatResponseFormatJson(null)));
        Assert.Equal(ChatResponseFormat.Json.GetHashCode(), new ChatResponseFormatJson(null).GetHashCode());

        Assert.True(new ChatResponseFormatJson("{}").Equals(new ChatResponseFormatJson("{}")));
        Assert.Equal(new ChatResponseFormatJson("{}").GetHashCode(), new ChatResponseFormatJson("{}").GetHashCode());

        Assert.False(new ChatResponseFormatJson("""{ "prop": 42 }""").Equals(new ChatResponseFormatJson("""{ "prop": 43 }""")));
        Assert.NotEqual(new ChatResponseFormatJson("""{ "prop": 42 }""").GetHashCode(), new ChatResponseFormatJson("""{ "prop": 43 }""").GetHashCode()); // technically not guaranteed

        Assert.False(new ChatResponseFormatJson("""{ "prop": 42 }""").Equals(new ChatResponseFormatJson("""{ "PROP": 42 }""")));
        Assert.NotEqual(new ChatResponseFormatJson("""{ "prop": 42 }""").GetHashCode(), new ChatResponseFormatJson("""{ "PROP": 42 }""").GetHashCode()); // technically not guaranteed

        Assert.True(new ChatResponseFormatJson("{}", "name", "description").Equals(new ChatResponseFormatJson("{}", "name", "description")));
        Assert.False(new ChatResponseFormatJson("{}", "name", "description").Equals(new ChatResponseFormatJson("{}", "name", "description2")));
        Assert.False(new ChatResponseFormatJson("{}", "name", "description").Equals(new ChatResponseFormatJson("{}", "name2", "description")));
        Assert.False(new ChatResponseFormatJson("{}", "name", "description").Equals(new ChatResponseFormatJson("{}", "name2", "description2")));

        Assert.Equal(new ChatResponseFormatJson("{}", "name", "description").GetHashCode(), new ChatResponseFormatJson("{}", "name", "description").GetHashCode());
    }

    [Fact]
    public void Serialization_TextRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatResponseFormat.Text, TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal("""{"$type":"text"}""", json);

        ChatResponseFormat? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal(ChatResponseFormat.Text, result);
    }

    [Fact]
    public void Serialization_JsonRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatResponseFormat.Json, TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal("""{"$type":"json"}""", json);

        ChatResponseFormat? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal(ChatResponseFormat.Json, result);
    }

    [Fact]
    public void Serialization_ForJsonSchemaRoundtrips()
    {
        string json = JsonSerializer.Serialize(ChatResponseFormat.ForJsonSchema("[1,2,3]", "name", "description"), TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal("""{"$type":"json","schema":"[1,2,3]","schemaName":"name","schemaDescription":"description"}""", json);

        ChatResponseFormat? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal(ChatResponseFormat.ForJsonSchema("[1,2,3]", "name", "description"), result);
        Assert.Equal("[1,2,3]", (result as ChatResponseFormatJson)?.Schema);
        Assert.Equal("name", (result as ChatResponseFormatJson)?.SchemaName);
        Assert.Equal("description", (result as ChatResponseFormatJson)?.SchemaDescription);
    }
}
