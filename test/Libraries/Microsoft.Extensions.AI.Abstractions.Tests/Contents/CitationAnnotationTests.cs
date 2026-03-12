// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class CitationAnnotationTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        CitationAnnotation a = new();
        Assert.Null(a.AdditionalProperties);
        Assert.Null(a.AnnotatedRegions);
        Assert.Null(a.RawRepresentation);
        Assert.Null(a.Snippet);
        Assert.Null(a.Title);
        Assert.Null(a.ToolName);
        Assert.Null(a.Url);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        CitationAnnotation a = new();

        Assert.Null(a.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        a.AdditionalProperties = props;
        Assert.Same(props, a.AdditionalProperties);

        Assert.Null(a.RawRepresentation);
        object raw = new();
        a.RawRepresentation = raw;
        Assert.Same(raw, a.RawRepresentation);

        Assert.Null(a.AnnotatedRegions);
        List<AnnotatedRegion> regions = [new TextSpanAnnotatedRegion { StartIndex = 10, EndIndex = 42 }];
        a.AnnotatedRegions = regions;
        Assert.Same(regions, a.AnnotatedRegions);

        Assert.Null(a.Snippet);
        a.Snippet = "snippet";
        Assert.Equal("snippet", a.Snippet);

        Assert.Null(a.Title);
        a.Title = "title";
        Assert.Equal("title", a.Title);

        Assert.Null(a.ToolName);
        a.ToolName = "toolName";
        Assert.Equal("toolName", a.ToolName);

        Assert.Null(a.Url);
        Uri url = new("https://example.com");
        a.Url = url;
        Assert.Same(url, a.Url);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        CitationAnnotation original = new()
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { { "key", "value" } },
            RawRepresentation = new Dictionary<string, object?> { ["value"] = 42 },
            Snippet = "snippet",
            Title = "title",
            ToolName = "toolName",
            Url = new("https://example.com"),
            AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = 10, EndIndex = 42 }],
        };

        string json = JsonSerializer.Serialize(original, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(CitationAnnotation)));
        Assert.NotNull(json);

        var deserialized = (CitationAnnotation?)JsonSerializer.Deserialize(json, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(CitationAnnotation)));
        Assert.NotNull(deserialized);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.Equal(JsonElement.Parse("\"value\"").ToString(), deserialized.AdditionalProperties["key"]!.ToString());

        JsonElement rawRepresentation = Assert.IsType<JsonElement>(deserialized.RawRepresentation);
        Assert.Equal(42, rawRepresentation.GetProperty("value").GetInt32());
        Assert.Equal("snippet", deserialized.Snippet);
        Assert.Equal("title", deserialized.Title);
        Assert.Equal("toolName", deserialized.ToolName);
        Assert.NotNull(deserialized.AnnotatedRegions);
        TextSpanAnnotatedRegion region = Assert.IsType<TextSpanAnnotatedRegion>(Assert.Single(deserialized.AnnotatedRegions));
        Assert.Equal(10, region.StartIndex);
        Assert.Equal(42, region.EndIndex);

        Assert.NotNull(deserialized.Url);
        Assert.Equal(original.Url, deserialized.Url);
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "$type": "citation",
              "title": "My Source",
              "url": "https://example.com/source",
              "fileId": "file-123",
              "toolName": "web_search",
              "snippet": "relevant excerpt",
              "annotatedRegions": [
                {
                  "$type": "textSpan",
                  "start": 10,
                  "end": 25
                }
              ],
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        AIAnnotation? result = JsonSerializer.Deserialize<AIAnnotation>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        var citation = Assert.IsType<CitationAnnotation>(result);
        Assert.Equal("My Source", citation.Title);
        Assert.Equal(new Uri("https://example.com/source"), citation.Url);
        Assert.Equal("file-123", citation.FileId);
        Assert.Equal("web_search", citation.ToolName);
        Assert.Equal("relevant excerpt", citation.Snippet);
        Assert.NotNull(citation.AnnotatedRegions);
        Assert.Single(citation.AnnotatedRegions);
        var textSpan = Assert.IsType<TextSpanAnnotatedRegion>(citation.AnnotatedRegions[0]);
        Assert.Equal(10, textSpan.StartIndex);
        Assert.Equal(25, textSpan.EndIndex);
        Assert.NotNull(citation.AdditionalProperties);
        Assert.Equal("val", citation.AdditionalProperties["key"]?.ToString());
    }
}
