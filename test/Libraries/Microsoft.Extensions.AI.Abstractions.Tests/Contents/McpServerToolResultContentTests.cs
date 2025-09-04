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
        Assert.Null(c.Output);
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

        Assert.Null(c.Output);
        IList<AIContent> output = [];
        c.Output = output;
        Assert.Same(output, c.Output);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("callId", () => new McpServerToolResultContent(string.Empty));
        Assert.Throws<ArgumentNullException>("callId", () => new McpServerToolResultContent(null!));
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializable()
    {
        var sut = new McpServerToolResultContent("id")
        {
            Output = [new TextContent("output text")]
        };

        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<McpServerToolResultContent>(json, TestJsonSerializerContext.Default.Options);

        Assert.NotNull(deserializedSut);
        Assert.Equal("id", deserializedSut.CallId);
        Assert.Equal("output text", Assert.IsType<TextContent>(Assert.Single(deserializedSut.Output!)).Text);
    }

    [Fact]
    public void ItShouldBeSerializableAndDeserializableAsPolymorphic()
    {
        AIContent sut = new McpServerToolResultContent("id")
        {
            Output = [new TextContent("output text")]
        };

        var json = JsonSerializer.Serialize(sut, TestJsonSerializerContext.Default.Options);
        var deserializedSut = JsonSerializer.Deserialize<AIContent>(json, TestJsonSerializerContext.Default.Options);

        var deserializedToolResultContent = Assert.IsType<McpServerToolResultContent>(deserializedSut);
        Assert.Equal("id", deserializedToolResultContent.CallId);
        Assert.Equal("output text", Assert.IsType<TextContent>(Assert.Single(deserializedToolResultContent.Output!)).Text);
    }
}
