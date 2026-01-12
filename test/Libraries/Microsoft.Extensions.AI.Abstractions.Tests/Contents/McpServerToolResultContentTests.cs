// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
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
        Assert.Null(c.Result);
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
        IList<AIContent> output = [];
        c.Result = output;
        Assert.Same(output, c.Result);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new McpServerToolResultContent(string.Empty));
        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolResultContent(null!));
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new McpServerToolResultContent("call123")
        {
            Result = new List<AIContent> { new TextContent("result") }
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<McpServerToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.CallId, deserializedContent.CallId);
        Assert.NotNull(deserializedContent.Result);
    }
}
