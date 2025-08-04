// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        HostedMcpServerToolResultContent c = new("callId");
        Assert.Equal("callId", c.CallId);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.Output);
        Assert.False(c.IsError);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        HostedMcpServerToolResultContent c = new("callId");

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("callId", c.CallId);

        Assert.Null(c.Output);
        IList<AIContent> output = [];
        c.Output = output;
        Assert.Same(output, c.Output);

        Assert.False(c.IsError);
        c.IsError = true;
        Assert.True(c.IsError);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new HostedMcpServerToolResultContent(string.Empty));
        Assert.Throws<ArgumentNullException>("callId", () => new HostedMcpServerToolResultContent(null!));
    }
}
