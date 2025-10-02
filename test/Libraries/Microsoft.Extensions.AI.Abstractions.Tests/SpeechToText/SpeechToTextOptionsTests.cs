// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        SpeechToTextOptions options = new();
        Assert.Null(options.ModelId);
        Assert.Null(options.SpeechLanguage);
        Assert.Null(options.SpeechSampleRate);
        Assert.Null(options.AdditionalProperties);

        SpeechToTextOptions clone = options.Clone();
        Assert.Null(clone.ModelId);
        Assert.Null(clone.SpeechLanguage);
        Assert.Null(clone.SpeechSampleRate);
        Assert.Null(clone.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        SpeechToTextOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        Func<ISpeechToTextClient?, object?> rawRepresentationFactory = (c) => null;

        options.ModelId = "modelId";
        options.SpeechLanguage = "en-US";
        options.SpeechSampleRate = 44100;
        options.AdditionalProperties = additionalProps;
        options.RawRepresentationFactory = rawRepresentationFactory;

        Assert.Equal("modelId", options.ModelId);
        Assert.Equal("en-US", options.SpeechLanguage);
        Assert.Equal(44100, options.SpeechSampleRate);
        Assert.Same(additionalProps, options.AdditionalProperties);
        Assert.Same(rawRepresentationFactory, options.RawRepresentationFactory);

        SpeechToTextOptions clone = options.Clone();
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal("en-US", clone.SpeechLanguage);
        Assert.Equal(44100, clone.SpeechSampleRate);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
        Assert.Same(rawRepresentationFactory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        SpeechToTextOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.ModelId = "modelId";
        options.SpeechLanguage = "en-US";
        options.SpeechSampleRate = 44100;
        options.AdditionalProperties = additionalProps;

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.SpeechToTextOptions);

        SpeechToTextOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.SpeechToTextOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("modelId", deserialized.ModelId);
        Assert.Equal("en-US", deserialized.SpeechLanguage);
        Assert.Equal(44100, deserialized.SpeechSampleRate);

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

        SpeechToTextOptions clone = b.Clone();

        Assert.Equal("test", clone.ModelId);
        Assert.Equal(42, Assert.IsType<OptionsA>(clone, exactMatch: false).A);
        Assert.Equal(84, Assert.IsType<OptionsB>(clone, exactMatch: true).B);
    }

    private class OptionsA : SpeechToTextOptions
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

        public override SpeechToTextOptions Clone() => new OptionsA(this);
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

        public override SpeechToTextOptions Clone() => new OptionsB(this);
    }

    [Fact]
    public void CopyConstructor_Null_Valid()
    {
        PassedNullToBaseOptions options = new();
        Assert.NotNull(options);
    }

    private class PassedNullToBaseOptions : SpeechToTextOptions
    {
        public PassedNullToBaseOptions()
            : base(null)
        {
        }
    }
}
