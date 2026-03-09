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
        WebSearchToolResultContent c = new("callId");
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal("callId", c.CallId);
        Assert.Null(c.Results);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        WebSearchToolResultContent c = new("ws_call123");

        Assert.Equal("ws_call123", c.CallId);

        Assert.Null(c.Results);
        IList<AIContent> results =
        [
            new UriContent(new Uri("https://example.com/1"), "text/html") { AdditionalProperties = new() { ["title"] = "Result 1" } }
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
        WebSearchToolResultContent c = new("ws_call789")
        {
            Results =
            [
                new UriContent(new Uri("https://example.com/1"), "text/html") { AdditionalProperties = new() { ["title"] = "First" } },
                new UriContent(new Uri("https://example.com/2"), "text/html") { AdditionalProperties = new() { ["title"] = "Second" } },
                new UriContent(new Uri("https://example.com/3"), "text/html"),
            ]
        };

        Assert.NotNull(c.Results);
        Assert.Equal(3, c.Results.Count);

        var first = Assert.IsType<UriContent>(c.Results[0]);
        Assert.Equal("First", first.AdditionalProperties?["title"]);

        var second = Assert.IsType<UriContent>(c.Results[1]);
        Assert.Equal("Second", second.AdditionalProperties?["title"]);

        var third = Assert.IsType<UriContent>(c.Results[2]);
        Assert.Null(third.AdditionalProperties);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        WebSearchToolResultContent content = new("ws_call123")
        {
            Results =
            [
                new UriContent(new Uri("https://example.com"), "text/html") { AdditionalProperties = new() { ["title"] = "Example Page" } },
                new UriContent(new Uri("https://another.com"), "text/html"),
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<WebSearchToolResultContent>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("ws_call123", deserialized.CallId);
        Assert.NotNull(deserialized.Results);
        Assert.Equal(2, deserialized.Results.Count);

        var first = Assert.IsType<UriContent>(deserialized.Results[0]);
        Assert.Equal(new Uri("https://example.com"), first.Uri);
        Assert.Equal("Example Page", first.AdditionalProperties?["title"]?.ToString());

        var second = Assert.IsType<UriContent>(deserialized.Results[1]);
        Assert.Equal(new Uri("https://another.com"), second.Uri);
    }

    [Fact]
    public void Serialization_AsAIContent_Roundtrips()
    {
        AIContent content = new WebSearchToolResultContent("ws_call456")
        {
            Results =
            [
                new UriContent(new Uri("https://test.com"), "text/html") { AdditionalProperties = new() { ["title"] = "Test" } },
            ]
        };

        var json = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<AIContent>(json, AIJsonUtilities.DefaultOptions);

        var result = Assert.IsType<WebSearchToolResultContent>(deserialized);
        Assert.Equal("ws_call456", result.CallId);
        Assert.NotNull(result.Results);
        Assert.Single(result.Results);

        var first = Assert.IsType<UriContent>(result.Results[0]);
        Assert.Equal("Test", first.AdditionalProperties?["title"]?.ToString());
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "$type": "webSearchToolResult",
              "callId": "ws-call1",
              "results": [
                {
                  "$type": "uri",
                  "uri": "https://example.com",
                  "mediaType": "text/html"
                }
              ],
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        AIContent? result = JsonSerializer.Deserialize<AIContent>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        var wsResult = Assert.IsType<WebSearchToolResultContent>(result);
        Assert.Equal("ws-call1", wsResult.CallId);
        Assert.NotNull(wsResult.Results);
        Assert.Single(wsResult.Results);
        var uriResult = Assert.IsType<UriContent>(wsResult.Results[0]);
        Assert.Equal(new Uri("https://example.com"), uriResult.Uri);
        Assert.Equal("text/html", uriResult.MediaType);
        Assert.NotNull(wsResult.AdditionalProperties);
        Assert.Equal("val", wsResult.AdditionalProperties["key"]?.ToString());
    }
}
