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
        McpServerToolResultContent c = new("call123");
        Assert.Equal("call123", c.Id);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.Output);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        McpServerToolResultContent c = new("call123");

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);

        Assert.Equal("call123", c.Id);

        Assert.Null(c.Output);
        IList<AIContent> output = [];
        c.Output = output;
        Assert.Same(output, c.Output);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("id", () => new McpServerToolResultContent(string.Empty));
        Assert.Throws<ArgumentNullException>("id", () => new McpServerToolResultContent(null!));
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        var content = new McpServerToolResultContent("call123")
        {
            Output = new List<AIContent> { new TextContent("result") }
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserializedContent = JsonSerializer.Deserialize<McpServerToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserializedContent);
        Assert.Equal(content.Id, deserializedContent.Id);
        Assert.NotNull(deserializedContent.Output);
        Assert.IsType<TextContent>(deserializedContent.Output[0]);
        Assert.Equal("result", ((TextContent)deserializedContent.Output[0]).Text);
    }
}
