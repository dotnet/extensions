// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class McpServerToolCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        McpServerToolCallContent c = new("callId1", "toolName", "serverName");

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("toolName", c.ToolName);
        Assert.Equal("serverName", c.ServerName);

        Assert.Null(c.Arguments);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        McpServerToolCallContent c = new("callId1", "toolName", "serverName");

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Null(c.Arguments);
        IReadOnlyDictionary<string, object?> args = new Dictionary<string, object?>();
        c.Arguments = args;
        Assert.Same(args, c.Arguments);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("toolName", c.ToolName);
        Assert.Equal("serverName", c.ServerName);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new McpServerToolCallContent(string.Empty, "name", "serverName"));
        Assert.Throws<ArgumentException>("toolName", () => new McpServerToolCallContent("callId1", string.Empty, "serverName"));
        Assert.Throws<ArgumentException>("serverName", () => new McpServerToolCallContent("callId1", "name", string.Empty));

        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolCallContent(null!, "name", "serverName"));
        Assert.Throws<ArgumentNullException>("toolName", () => new McpServerToolCallContent("callId1", null!, "serverName"));
        Assert.Throws<ArgumentNullException>("serverName", () => new McpServerToolCallContent("callId1", "name", null!));
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializable()
    {
        var sut = new McpServerToolCallContent("id", "toolName", "serverName");

        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<McpServerToolCallContent>(json, TestJsonSerializerContext.Default.Options);

        Assert.NotNull(deserializedSut);
        Assert.Equal(sut.CallId, deserializedSut.CallId);
        Assert.Equal(sut.ToolName, deserializedSut.ToolName);
        Assert.Equal(sut.ServerName, deserializedSut.ServerName);
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializableAsPolymorphic()
    {
        AIContent sut = new McpServerToolCallContent("id", "toolName", "serverName");

        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<AIContent>(json, TestJsonSerializerContext.Default.Options);

        var toolCallContent = (McpServerToolCallContent)sut;
        var deserializedToolCallContent = Assert.IsType<McpServerToolCallContent>(deserializedSut);
        Assert.Equal(toolCallContent.CallId, deserializedToolCallContent.CallId);
        Assert.Equal(toolCallContent.ToolName, deserializedToolCallContent.ToolName);
        Assert.Equal(toolCallContent.ServerName, deserializedToolCallContent.ServerName);
    }
}
