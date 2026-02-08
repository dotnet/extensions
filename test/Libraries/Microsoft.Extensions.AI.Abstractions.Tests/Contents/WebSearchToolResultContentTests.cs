// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class WebSearchToolResultContentTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        WebSearchToolResultContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.CallId);
        Assert.Null(c.Results);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        WebSearchToolResultContent c = new();

        Assert.Null(c.CallId);
        c.CallId = "ws_call123";
        Assert.Equal("ws_call123", c.CallId);

        Assert.Null(c.Results);
        IList<WebSearchResult> results =
        [
            new WebSearchResult { Title = "Result 1", Url = new Uri("https://example.com/1"), Snippet = "First result" }
        ];
        c.Results = results;
        Assert.Same(results, c.Results);

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
    public void Results_SupportsMultipleItems()
    {
        WebSearchToolResultContent c = new()
        {
            CallId = "ws_call789",
            Results =
            [
                new WebSearchResult { Title = "First", Url = new Uri("https://example.com/1"), Snippet = "First snippet" },
                new WebSearchResult { Title = "Second", Url = new Uri("https://example.com/2"), Snippet = "Second snippet" },
                new WebSearchResult { Title = "Third", Url = new Uri("https://example.com/3") },
            ]
        };

        Assert.NotNull(c.Results);
        Assert.Equal(3, c.Results.Count);
        Assert.Equal("First", c.Results[0].Title);
        Assert.Equal("Second", c.Results[1].Title);
        Assert.Null(c.Results[2].Snippet);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        WebSearchToolResultContent content = new()
        {
            CallId = "ws_call123",
            Results =
            [
                new WebSearchResult { Title = "Example Page", Url = new Uri("https://example.com"), Snippet = "An example" },
                new WebSearchResult { Title = "Another Page", Url = new Uri("https://another.com") },
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<WebSearchToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("ws_call123", deserialized.CallId);
        Assert.NotNull(deserialized.Results);
        Assert.Equal(2, deserialized.Results.Count);
        Assert.Equal("Example Page", deserialized.Results[0].Title);
        Assert.Equal(new Uri("https://example.com"), deserialized.Results[0].Url);
        Assert.Equal("An example", deserialized.Results[0].Snippet);
        Assert.Equal("Another Page", deserialized.Results[1].Title);
        Assert.Equal(new Uri("https://another.com"), deserialized.Results[1].Url);
        Assert.Null(deserialized.Results[1].Snippet);
    }

    [Fact]
    public void Serialization_AsAIContent_Roundtrips()
    {
        AIContent content = new WebSearchToolResultContent
        {
            CallId = "ws_call456",
            Results =
            [
                new WebSearchResult { Title = "Test", Url = new Uri("https://test.com"), Snippet = "A test" },
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);

        var result = Assert.IsType<WebSearchToolResultContent>(deserialized);
        Assert.Equal("ws_call456", result.CallId);
        Assert.NotNull(result.Results);
        Assert.Single(result.Results);
        Assert.Equal("Test", result.Results[0].Title);
    }
}
