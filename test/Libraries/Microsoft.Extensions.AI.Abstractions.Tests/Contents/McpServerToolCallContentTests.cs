// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class McpServerToolCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        McpServerToolCallContent c = new("callId1");

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        Assert.Null(c.ToolName);
        Assert.Null(c.ServerName);

        Assert.Null(c.Arguments);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        McpServerToolCallContent c = new("callId1");

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

        Assert.Null(c.ToolName);
        c.ToolName = "toolName";
        Assert.Equal("toolName", c.ToolName);

        Assert.Null(c.ServerName);
        c.ServerName = "serverName";
        Assert.Equal("serverName", c.ServerName);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new McpServerToolCallContent(string.Empty));
        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolCallContent(null!));
    }
}
