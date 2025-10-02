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
        Assert.Null(options.ModelId);
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.Dimensions);

        EmbeddingGenerationOptions clone = options.Clone();
        Assert.Null(clone.ModelId);
        Assert.Null(clone.AdditionalProperties);
        Assert.Null(clone.Dimensions);
    }

    [Fact]
    public void InvalidArgs_Throws()
    {
        EmbeddingGenerationOptions options = new();
        Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Dimensions = 0);
        Assert.Throws<ArgumentOutOfRangeException>("value", () => options.Dimensions = -1);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        EmbeddingGenerationOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        Func<IEmbeddingGenerator?, object?> rawRepresentationFactory = (c) => null;

        options.ModelId = "modelId";
        options.Dimensions = 1536;
        options.AdditionalProperties = additionalProps;
        options.RawRepresentationFactory = rawRepresentationFactory;

        Assert.Equal("modelId", options.ModelId);
        Assert.Equal(1536, options.Dimensions);
        Assert.Same(additionalProps, options.AdditionalProperties);
        Assert.Same(rawRepresentationFactory, options.RawRepresentationFactory);

        EmbeddingGenerationOptions clone = options.Clone();
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal(1536, clone.Dimensions);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
        Assert.Same(rawRepresentationFactory, clone.RawRepresentationFactory);
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

    [Fact]
    public void CopyConstructors_EnableHeirarchyCloning()
    {
        OptionsB b = new()
        {
            ModelId = "test",
            A = 42,
            B = 84,
        };

        EmbeddingGenerationOptions clone = b.Clone();

        Assert.Equal("test", clone.ModelId);
        Assert.Equal(42, Assert.IsType<OptionsA>(clone, exactMatch: false).A);
        Assert.Equal(84, Assert.IsType<OptionsB>(clone, exactMatch: true).B);
    }

    private class OptionsA : EmbeddingGenerationOptions
    {
        public OptionsA()
        {
        }

        protected OptionsA(OptionsA other)
            : base(other)
        {
            A = other.A;
        }

        public int A { get; set; }

        public override EmbeddingGenerationOptions Clone() => new OptionsA(this);
    }

    private class OptionsB : OptionsA
    {
        public OptionsB()
        {
        }

        protected OptionsB(OptionsB other)
            : base(other)
        {
            B = other.B;
        }

        public int B { get; set; }

        public override EmbeddingGenerationOptions Clone() => new OptionsB(this);
    }

    [Fact]
    public void CopyConstructor_Null_Valid()
    {
        PassedNullToBaseOptions options = new();
        Assert.NotNull(options);
    }

    private class PassedNullToBaseOptions : EmbeddingGenerationOptions
    {
        public PassedNullToBaseOptions()
            : base(null)
        {
        }
    }
}
