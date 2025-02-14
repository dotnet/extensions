// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        AudioTranscriptionOptions options = new();
        Assert.Null(options.TranscriptionId);
        Assert.Null(options.ModelId);
        Assert.Null(options.AudioLanguage);
        Assert.Null(options.AudioSampleRate);
        Assert.Null(options.AdditionalProperties);

        AudioTranscriptionOptions clone = options.Clone();
        Assert.Null(clone.TranscriptionId);
        Assert.Null(clone.ModelId);
        Assert.Null(clone.AudioLanguage);
        Assert.Null(clone.AudioSampleRate);
        Assert.Null(clone.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        AudioTranscriptionOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.TranscriptionId = "completionId";
        options.ModelId = "modelId";
        options.AudioLanguage = "en-US";
        options.AudioSampleRate = 44100;
        options.AdditionalProperties = additionalProps;

        Assert.Equal("completionId", options.TranscriptionId);
        Assert.Equal("modelId", options.ModelId);
        Assert.Equal("en-US", options.AudioLanguage);
        Assert.Equal(44100, options.AudioSampleRate);
        Assert.Same(additionalProps, options.AdditionalProperties);

        AudioTranscriptionOptions clone = options.Clone();
        Assert.Equal("completionId", clone.TranscriptionId);
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal("en-US", clone.AudioLanguage);
        Assert.Equal(44100, clone.AudioSampleRate);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        AudioTranscriptionOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.TranscriptionId = "completionId";
        options.ModelId = "modelId";
        options.AudioLanguage = "en-US";
        options.AudioSampleRate = 44100;
        options.AdditionalProperties = additionalProps;

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.AudioTranscriptionOptions);

        AudioTranscriptionOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.AudioTranscriptionOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("completionId", deserialized.TranscriptionId);
        Assert.Equal("modelId", deserialized.ModelId);
        Assert.Equal("en-US", deserialized.AudioLanguage);
        Assert.Equal(44100, deserialized.AudioSampleRate);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.True(deserialized.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public void AudioLanguage_InvalidCulture_ThrowsCultureNotFoundException(string invalidCulture)
    {
        AudioTranscriptionOptions options = new();
        Assert.Throws<CultureNotFoundException>(() => options.AudioLanguage = invalidCulture);
    }

    [Fact]
    public void AudioLanguage_EmptyString_SetsInvariantCulture()
    {
        AudioTranscriptionOptions options = new()
        {
            AudioLanguage = string.Empty,
        };

        // InvariantCulture's Name is returned when an empty string is used.
        Assert.Equal(CultureInfo.InvariantCulture.Name, options.AudioLanguage);
    }
}
