// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        HostedMcpServerToolCallContent c = new("callId1", "toolName", "serverName");

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
        HostedMcpServerToolCallContent c = new("callId1", "toolName", "serverName");

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
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("callId1", c.CallId);
        Assert.Equal("toolName", c.ToolName);
        Assert.Equal("serverName", c.ServerName);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new HostedMcpServerToolCallContent(string.Empty, "name", "serverName"));
        Assert.Throws<ArgumentException>("toolName", () => new HostedMcpServerToolCallContent("callId1", string.Empty, "serverName"));
        Assert.Throws<ArgumentException>("serverName", () => new HostedMcpServerToolCallContent("callId1", "name", string.Empty));

        Assert.Throws<ArgumentNullException>("callId", () => new HostedMcpServerToolCallContent(null!, "name", "serverName"));
        Assert.Throws<ArgumentNullException>("toolName", () => new HostedMcpServerToolCallContent("callId1", null!, "serverName"));
        Assert.Throws<ArgumentNullException>("serverName", () => new HostedMcpServerToolCallContent("callId1", "name", null!));
    }
}
