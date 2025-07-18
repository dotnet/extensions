// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        Assert.Null(a.EndIndex);
        Assert.Null(a.RawRepresentation);
        Assert.Null(a.StartIndex);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        AIAnnotation a = new();

        Assert.Null(a.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        a.AdditionalProperties = props;
        Assert.Same(props, a.AdditionalProperties);

        Assert.Null(a.EndIndex);
        a.EndIndex = 42;
        Assert.Equal(42, a.EndIndex);

        Assert.Null(a.RawRepresentation);
        object raw = new();
        a.RawRepresentation = raw;
        Assert.Same(raw, a.RawRepresentation);

        Assert.Null(a.StartIndex);
        a.StartIndex = 10;
        Assert.Equal(10, a.StartIndex);
    }

    [Fact]
    public void Serialization_Roundtrips()
    {
        AIAnnotation original = new()
        {
            AdditionalProperties = new AdditionalPropertiesDictionary { { "key", "value" } },
            EndIndex = 42,
            RawRepresentation = new object(),
            StartIndex = 10,
        };

        string json = JsonSerializer.Serialize(original, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(AIAnnotation)));
        Assert.NotNull(json);

        AIAnnotation? deserialized = (AIAnnotation?)JsonSerializer.Deserialize(json, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(AIAnnotation)));
        Assert.NotNull(deserialized);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.Equal(JsonSerializer.Deserialize<JsonElement>("\"value\"", AIJsonUtilities.DefaultOptions).ToString(), deserialized.AdditionalProperties["key"]!.ToString());

        Assert.Equal(42, deserialized.EndIndex);
        Assert.Null(deserialized.RawRepresentation);
        Assert.Equal(10, deserialized.StartIndex);
    }
}
