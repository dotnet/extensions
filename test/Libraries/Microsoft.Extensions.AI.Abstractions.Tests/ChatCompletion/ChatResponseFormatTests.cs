// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatResponseFormatTests
{
    private static JsonElement EmptySchema => JsonDocument.Parse("{}").RootElement;

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
        ChatResponseFormatJson f = new(EmptySchema, "name", "description");
        Assert.Equal("{}", JsonSerializer.Serialize(f.Schema, TestJsonSerializerContext.Default.JsonElement));
        Assert.Equal("name", f.SchemaName);
        Assert.Equal("description", f.SchemaDescription);
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
        var actual = Assert.IsType<ChatResponseFormatJson>(result);
        Assert.Null(actual.Schema);
        Assert.Null(actual.SchemaDescription);
        Assert.Null(actual.SchemaName);
    }

    [Fact]
    public void Serialization_ForJsonSchemaRoundtrips()
    {
        string json = JsonSerializer.Serialize(
            ChatResponseFormat.ForJsonSchema(JsonSerializer.Deserialize<JsonElement>("[1,2,3]"), "name", "description"),
            TestJsonSerializerContext.Default.ChatResponseFormat);
        Assert.Equal("""{"$type":"json","schema":[1,2,3],"schemaName":"name","schemaDescription":"description"}""", json);

        ChatResponseFormat? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ChatResponseFormat);
        var actual = Assert.IsType<ChatResponseFormatJson>(result);
        Assert.Equal("[1,2,3]", JsonSerializer.Serialize(actual.Schema, TestJsonSerializerContext.Default.JsonElement));
        Assert.Equal("name", actual.SchemaName);
        Assert.Equal("description", actual.SchemaDescription);
    }
}
