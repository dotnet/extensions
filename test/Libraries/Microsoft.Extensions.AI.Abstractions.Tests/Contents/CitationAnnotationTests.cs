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
            RawRepresentation = new object(),
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

        Assert.Null(deserialized.RawRepresentation);
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
}
