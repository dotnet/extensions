// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        TextToSpeechOptions options = new();
        Assert.Null(options.ModelId);
        Assert.Null(options.VoiceId);
        Assert.Null(options.Language);
        Assert.Null(options.AudioFormat);
        Assert.Null(options.Speed);
        Assert.Null(options.Pitch);
        Assert.Null(options.Volume);
        Assert.Null(options.AdditionalProperties);

        TextToSpeechOptions clone = options.Clone();
        Assert.Null(clone.ModelId);
        Assert.Null(clone.VoiceId);
        Assert.Null(clone.Language);
        Assert.Null(clone.AudioFormat);
        Assert.Null(clone.Speed);
        Assert.Null(clone.Pitch);
        Assert.Null(clone.Volume);
        Assert.Null(clone.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        TextToSpeechOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        Func<ITextToSpeechClient?, object?> rawRepresentationFactory = (c) => null;

        options.ModelId = "modelId";
        options.VoiceId = "alloy";
        options.Language = "en-US";
        options.AudioFormat = "audio/mpeg";
        options.Speed = 1.5f;
        options.Pitch = 0.8f;
        options.Volume = 0.9f;
        options.AdditionalProperties = additionalProps;
        options.RawRepresentationFactory = rawRepresentationFactory;

        Assert.Equal("modelId", options.ModelId);
        Assert.Equal("alloy", options.VoiceId);
        Assert.Equal("en-US", options.Language);
        Assert.Equal("audio/mpeg", options.AudioFormat);
        Assert.Equal(1.5f, options.Speed);
        Assert.Equal(0.8f, options.Pitch);
        Assert.Equal(0.9f, options.Volume);
        Assert.Same(additionalProps, options.AdditionalProperties);
        Assert.Same(rawRepresentationFactory, options.RawRepresentationFactory);

        TextToSpeechOptions clone = options.Clone();
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal("alloy", clone.VoiceId);
        Assert.Equal("en-US", clone.Language);
        Assert.Equal("audio/mpeg", clone.AudioFormat);
        Assert.Equal(1.5f, clone.Speed);
        Assert.Equal(0.8f, clone.Pitch);
        Assert.Equal(0.9f, clone.Volume);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
        Assert.Same(rawRepresentationFactory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        TextToSpeechOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.ModelId = "modelId";
        options.VoiceId = "alloy";
        options.Language = "en-US";
        options.AudioFormat = "mp3";
        options.Speed = 1.5f;
        options.Pitch = 0.8f;
        options.Volume = 0.9f;
        options.AdditionalProperties = additionalProps;

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.TextToSpeechOptions);

        TextToSpeechOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToSpeechOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("modelId", deserialized.ModelId);
        Assert.Equal("alloy", deserialized.VoiceId);
        Assert.Equal("en-US", deserialized.Language);
        Assert.Equal("mp3", deserialized.AudioFormat);
        Assert.Equal(1.5f, deserialized.Speed);
        Assert.Equal(0.8f, deserialized.Pitch);
        Assert.Equal(0.9f, deserialized.Volume);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.True(deserialized.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }

    [Fact]
    public void CopyConstructors_EnableHierarchyCloning()
    {
        OptionsB b = new()
        {
            ModelId = "test",
            A = 42,
            B = 84,
        };

        TextToSpeechOptions clone = b.Clone();

        Assert.Equal("test", clone.ModelId);
        Assert.Equal(42, Assert.IsType<OptionsA>(clone, exactMatch: false).A);
        Assert.Equal(84, Assert.IsType<OptionsB>(clone, exactMatch: true).B);
    }

    private class OptionsA : TextToSpeechOptions
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

        public override TextToSpeechOptions Clone() => new OptionsA(this);
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

        public override TextToSpeechOptions Clone() => new OptionsB(this);
    }

    [Fact]
    public void CopyConstructor_Null_Valid()
    {
        PassedNullToBaseOptions options = new();
        Assert.NotNull(options);
    }

    private class PassedNullToBaseOptions : TextToSpeechOptions
    {
        public PassedNullToBaseOptions()
            : base(null)
        {
        }
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "modelId": "tts-1",
              "voiceId": "alloy",
              "language": "en-US",
              "audioFormat": "mp3",
              "speed": 1.5,
              "pitch": 0.8,
              "volume": 0.9,
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        TextToSpeechOptions? result = JsonSerializer.Deserialize<TextToSpeechOptions>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        Assert.Equal("tts-1", result.ModelId);
        Assert.Equal("alloy", result.VoiceId);
        Assert.Equal("en-US", result.Language);
        Assert.Equal("mp3", result.AudioFormat);
        Assert.Equal(1.5f, result.Speed);
        Assert.Equal(0.8f, result.Pitch);
        Assert.Equal(0.9f, result.Volume);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal("val", result.AdditionalProperties["key"]?.ToString());
    }
}
