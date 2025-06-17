// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGenerationOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        EmbeddingGenerationOptions options = new();
        AssertDefaults(options);
        AssertDefaults(options.Clone());
    }

    private static void AssertDefaults(EmbeddingGenerationOptions options)
    {
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.Dimensions);
        Assert.Null(options.ModelId);
        Assert.Null(options.RawRepresentationFactory);
    }

    [Fact]
    public void InvalidArgs_Throws()
    {
        EmbeddingGenerationOptions options = new();
        Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Dimensions = 0);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Dimensions = -1);
    }

    [Fact]
    public void Merge_MembersCopiedOver_Default()
    {
        using TestEmbeddingGenerator g1 = new();
        using TestEmbeddingGenerator g2 = new();
        using TestEmbeddingGenerator g3 = new();

        EmbeddingGenerationOptions options = new();
        AssertDefaults(options);

        options.Merge(null);
        AssertDefaults(options);

        options.Merge(new EmbeddingGenerationOptions());
        AssertDefaults(options);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "value", ["key2"] = "value2" },
            Dimensions = 1536,
            ModelId = "modelId",
            RawRepresentationFactory = c => c == g1 ? new FormatException() : null,
        });

        Assert.Equal(1536, options.Dimensions);
        Assert.Equal("modelId", options.ModelId);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(2, options.AdditionalProperties.Count);
        Assert.Equal("value", options.AdditionalProperties["key"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(g1));
        Assert.Null(options.RawRepresentationFactory?.Invoke(g2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(g3));

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "changedvalue", ["key3"] = "value3" },
            Dimensions = 386,
            ModelId = "modelId2",
            RawRepresentationFactory = c => c == g2 ? new ArgumentException() : null,
        });

        Assert.Equal(1536, options.Dimensions);
        Assert.Equal("modelId", options.ModelId);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(3, options.AdditionalProperties.Count);
        Assert.Equal("value", options.AdditionalProperties["key"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(g1));
        Assert.IsType<ArgumentException>(options.RawRepresentationFactory?.Invoke(g2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(g3));
    }

    [Fact]
    public void Merge_MembersCopiedOver_Overwrite()
    {
        using TestEmbeddingGenerator g1 = new();
        using TestEmbeddingGenerator g2 = new();
        using TestEmbeddingGenerator g3 = new();

        EmbeddingGenerationOptions options = new();
        AssertDefaults(options);

        options.Merge(null, overwrite: true);
        AssertDefaults(options);

        options.Merge(new EmbeddingGenerationOptions(), overwrite: true);
        AssertDefaults(options);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "value", ["key2"] = "value2" },
            Dimensions = 1536,
            ModelId = "modelId",
            RawRepresentationFactory = c => c == g1 ? new FormatException() : null,
        }, overwrite: true);

        Assert.Equal(1536, options.Dimensions);
        Assert.Equal("modelId", options.ModelId);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(2, options.AdditionalProperties.Count);
        Assert.Equal("value", options.AdditionalProperties["key"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(g1));
        Assert.Null(options.RawRepresentationFactory?.Invoke(g2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(g3));

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "changedvalue", ["key3"] = "value3" },
            Dimensions = 386,
            ModelId = "modelId2",
            RawRepresentationFactory = c => c == g2 ? new ArgumentException() : null,
        }, overwrite: true);

        Assert.Equal(386, options.Dimensions);
        Assert.Equal("modelId2", options.ModelId);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(3, options.AdditionalProperties.Count);
        Assert.Equal("changedvalue", options.AdditionalProperties["key"]);
        Assert.Equal("value2", options.AdditionalProperties["key2"]);
        Assert.Equal("value3", options.AdditionalProperties["key3"]);
        Assert.Null(options.RawRepresentationFactory?.Invoke(g1));
        Assert.IsType<ArgumentException>(options.RawRepresentationFactory?.Invoke(g2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(g3));
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        EmbeddingGenerationOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.ModelId = "modelId";
        options.Dimensions = 1536;
        options.AdditionalProperties = additionalProps;

        Assert.Equal("modelId", options.ModelId);
        Assert.Equal(1536, options.Dimensions);
        Assert.Same(additionalProps, options.AdditionalProperties);

        EmbeddingGenerationOptions clone = options.Clone();
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal(1536, clone.Dimensions);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        EmbeddingGenerationOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.ModelId = "model";
        options.AdditionalProperties = additionalProps;
        options.Dimensions = 1536;

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.EmbeddingGenerationOptions);

        EmbeddingGenerationOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.EmbeddingGenerationOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("model", deserialized.ModelId);
        Assert.Equal(1536, deserialized.Dimensions);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.True(deserialized.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }
}
