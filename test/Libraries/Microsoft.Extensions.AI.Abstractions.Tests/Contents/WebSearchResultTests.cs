// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class WebSearchResultTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        WebSearchResult r = new();
        Assert.Null(r.Title);
        Assert.Null(r.Url);
        Assert.Null(r.Snippet);
        Assert.Null(r.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        WebSearchResult r = new();

        Assert.Null(r.Title);
        r.Title = "Example Page";
        Assert.Equal("Example Page", r.Title);

        Assert.Null(r.Url);
        Uri url = new("https://example.com/page");
        r.Url = url;
        Assert.Same(url, r.Url);

        Assert.Null(r.Snippet);
        r.Snippet = "A snippet from the page.";
        Assert.Equal("A snippet from the page.", r.Snippet);

        Assert.Null(r.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "domain", "example.com" } };
        r.AdditionalProperties = props;
        Assert.Same(props, r.AdditionalProperties);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        WebSearchResult result = new()
        {
            Title = "Test Page",
            Url = new Uri("https://test.com/article"),
            Snippet = "A snippet from the test page.",
        };

        var json = JsonSerializer.Serialize(result, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<WebSearchResult>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("Test Page", deserialized.Title);
        Assert.Equal(new Uri("https://test.com/article"), deserialized.Url);
        Assert.Equal("A snippet from the test page.", deserialized.Snippet);
    }

    [Fact]
    public void Serialization_WithNullOptionalProperties_Roundtrips()
    {
        WebSearchResult result = new()
        {
            Title = "Minimal Result",
            Url = new Uri("https://minimal.com"),
        };

        var json = JsonSerializer.Serialize(result, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<WebSearchResult>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("Minimal Result", deserialized.Title);
        Assert.Equal(new Uri("https://minimal.com"), deserialized.Url);
        Assert.Null(deserialized.Snippet);
    }
}
