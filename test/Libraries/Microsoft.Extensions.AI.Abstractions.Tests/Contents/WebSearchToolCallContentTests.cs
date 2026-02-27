// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class WebSearchToolCallContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        WebSearchToolCallContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.CallId);
        Assert.Null(c.Queries);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        WebSearchToolCallContent c = new();

        Assert.Null(c.CallId);
        c.CallId = "ws_call123";
        Assert.Equal("ws_call123", c.CallId);

        Assert.Null(c.Queries);
        c.Queries = ["latest .NET news", "dotnet 10 release"];
        Assert.Equal(2, c.Queries.Count);
        Assert.Equal("latest .NET news", c.Queries[0]);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        WebSearchToolCallContent content = new()
        {
            CallId = "ws_call123",
            Queries = ["what is .NET 10"],
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<WebSearchToolCallContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("ws_call123", deserialized.CallId);
        Assert.Equal(["what is .NET 10"], deserialized.Queries);
    }

    [Fact]
    public void Serialization_AsAIContent_Roundtrips()
    {
        AIContent content = new WebSearchToolCallContent
        {
            CallId = "ws_call456",
            Queries = ["AI safety research", "latest AI alignment papers"],
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);

        var result = Assert.IsType<WebSearchToolCallContent>(deserialized);
        Assert.Equal("ws_call456", result.CallId);
        Assert.Equal(["AI safety research", "latest AI alignment papers"], result.Queries);
    }
}
