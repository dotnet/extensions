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
        McpServerToolCallContent c = new("callId1", "toolName", null);

        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("toolName", c.ToolName);
        Assert.Equal("toolName", c.Name); // Name is inherited from FunctionCallContent
        Assert.Null(c.ServerName);
        Assert.Null(c.Arguments);
        Assert.False(c.InvocationRequired); // Should be false for MCP server tool calls
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
        Assert.Equal("toolName", c.ToolName);
        Assert.Equal("serverName", c.ServerName);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new McpServerToolCallContent(string.Empty, "name", null));
        Assert.Throws<ArgumentException>("toolName", () => new McpServerToolCallContent("callId1", string.Empty, null));

        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolCallContent(null!, "name", null));
        Assert.Throws<ArgumentNullException>("toolName", () => new McpServerToolCallContent("callId1", null!, null));
    }

    [Fact]
    public void InheritsFromFunctionCallContent()
    {
        McpServerToolCallContent c = new("callId1", "toolName", "serverName");

        // Verify it's a FunctionCallContent
        Assert.IsAssignableFrom<FunctionCallContent>(c);

        // InvocationRequired should be false for MCP server tool calls
        Assert.False(c.InvocationRequired);

        // The Name property should be the same as ToolName
        Assert.Equal(c.ToolName, c.Name);
    }

    [Fact]
    public void InvocationRequired_IsFalse()
    {
        // McpServerToolCallContent should always have InvocationRequired = false
        // because it represents calls that are already handled by the service
        McpServerToolCallContent c = new("callId1", "toolName", null);

        Assert.False(c.InvocationRequired);
    }
}
