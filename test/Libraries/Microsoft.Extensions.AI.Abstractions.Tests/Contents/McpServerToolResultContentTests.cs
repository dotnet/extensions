// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class McpServerToolResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        McpServerToolResultContent c = new("callId");
        Assert.Equal("callId", c.CallId);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.Result); // Now using inherited Result from FunctionResultContent
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        McpServerToolResultContent c = new("callId");

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("callId", c.CallId);

        Assert.Null(c.Result);
        object result = new();
        c.Result = result;
        Assert.Same(result, c.Result);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new McpServerToolResultContent(string.Empty));
        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolResultContent(null!));
    }

    [Fact]
    public void InheritsFromFunctionResultContent()
    {
        McpServerToolResultContent c = new("callId");

        // Verify it's a FunctionResultContent
        Assert.IsAssignableFrom<FunctionResultContent>(c);

        // The CallId property should be the same
        Assert.Equal("callId", c.CallId);
    }
}
