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
        McpServerToolCallContent c = new("call123", "toolName", null);

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("call123", c.Id);
        Assert.Equal("toolName", c.ToolName);
        Assert.Null(c.ServerName);
        Assert.Null(c.Arguments);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        McpServerToolCallContent c = new("call123", "toolName", "serverName");

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

        Assert.Equal("call123", c.Id);
        Assert.Equal("toolName", c.ToolName);
        Assert.Equal("serverName", c.ServerName);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("id", () => new McpServerToolCallContent(string.Empty, "name", null));
        Assert.Throws<ArgumentException>("toolName", () => new McpServerToolCallContent("callId1", string.Empty, null));

        Assert.Throws<ArgumentNullException>("id", () => new McpServerToolCallContent(null!, "name", null));
        Assert.Throws<ArgumentNullException>("toolName", () => new McpServerToolCallContent("callId1", null!, null));
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new McpServerToolCallContent("call123", "toolName", "serverName")
        {
            Arguments = new Dictionary<string, object?>
            {
                { "arg1", 123 },
                { "arg2", "456" }
            }
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<McpServerToolCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.Id, deserializedContent.Id);
        Assert.Equal(content.ToolName, deserializedContent.ToolName);
        Assert.Equal(content.ServerName, deserializedContent.ServerName);
        Assert.NotNull(deserializedContent.Arguments);
        Assert.Equal(2, deserializedContent.Arguments.Count);
        Assert.Collection(deserializedContent.Arguments,
            kvp =>
            {
                Assert.Equal("arg1", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.Number });
            },
            kvp =>
            {
                Assert.Equal("arg2", kvp.Key);
                Assert.True(kvp.Value is JsonElement { ValueKind: JsonValueKind.String });
            });
    }
}
