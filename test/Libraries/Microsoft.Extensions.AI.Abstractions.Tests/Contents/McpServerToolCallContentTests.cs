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
        McpServerToolCallContent c = new("callId1", "toolName", null);

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("toolName", c.Name);
        Assert.Null(c.ServerName);
        Assert.Null(c.Arguments);
        Assert.False(c.InvocationRequired);
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
        IDictionary<string, object?> args = new Dictionary<string, object?>();
        c.Arguments = args;
        Assert.Same(args, c.Arguments);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("toolName", c.Name);
        Assert.Equal("serverName", c.ServerName);
        Assert.False(c.InvocationRequired);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolCallContent(null!, "name", null));
        Assert.Throws<ArgumentNullException>("name", () => new McpServerToolCallContent("callId1", null!, null));
    }

    [Fact]
    public void Constructor_EmptyCallId_Accepted()
    {
        McpServerToolCallContent c = new(string.Empty, "toolName", "serverName");

        Assert.Equal(string.Empty, c.CallId);
        Assert.Equal("toolName", c.Name);
        Assert.Equal("serverName", c.ServerName);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new McpServerToolCallContent("call123", "myTool", "myServer")
        {
            Arguments = new Dictionary<string, object?> { { "arg1", "value1" } }
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<McpServerToolCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.CallId, deserializedContent.CallId);
        Assert.Equal(content.Name, deserializedContent.Name);
        Assert.Equal(content.ServerName, deserializedContent.ServerName);
        Assert.NotNull(deserializedContent.Arguments);
        Assert.Equal("value1", deserializedContent.Arguments["arg1"]?.ToString());
    }
}
