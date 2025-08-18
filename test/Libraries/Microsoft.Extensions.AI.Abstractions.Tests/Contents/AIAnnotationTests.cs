// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIAnnotationTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        AIAnnotation a = new();
        Assert.Null(a.AdditionalProperties);
        Assert.Null(a.RawRepresentation);
        Assert.Null(a.AnnotatedRegions);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        AIAnnotation a = new();

        Assert.Null(a.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        a.AdditionalProperties = props;
        Assert.Same(props, a.AdditionalProperties);

        Assert.Null(a.AnnotatedRegions);
        List<AnnotatedRegion> regions = [new TextSpanAnnotatedRegion { StartIndex = 10, EndIndex = 42 }];
        a.AnnotatedRegions = regions;
        Assert.Same(regions, a.AnnotatedRegions);

        Assert.Null(a.RawRepresentation);
        object raw = new();
        a.RawRepresentation = raw;
        Assert.Same(raw, a.RawRepresentation);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        AIAnnotation original = new()
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { { "key", "value" } },
            AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = 10, EndIndex = 42 }],
            RawRepresentation = new object(),
        };

        string json = JsonSerializer.Serialize(original, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(AIAnnotation)));
        Assert.NotNull(json);

        var deserialized = (AIAnnotation?)JsonSerializer.Deserialize(json, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(AIAnnotation)));
        Assert.NotNull(deserialized);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.Equal(JsonSerializer.Deserialize<JsonElement>("\"value\"", AIJsonUtilities.DefaultOptions).ToString(), deserialized.AdditionalProperties["key"]!.ToString());

        Assert.Null(deserialized.RawRepresentation);

        Assert.NotNull(deserialized.AnnotatedRegions);
        TextSpanAnnotatedRegion? region = Assert.IsType<TextSpanAnnotatedRegion>(Assert.Single(deserialized.AnnotatedRegions));
        Assert.NotNull(region);
        Assert.Equal(10, region.StartIndex);
        Assert.Equal(42, region.EndIndex);
    }
}
