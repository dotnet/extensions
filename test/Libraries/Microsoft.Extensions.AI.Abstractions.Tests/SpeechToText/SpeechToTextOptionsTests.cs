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
        AssertDefaults(options);
        AssertDefaults(options.Clone());
    }

    private static void AssertDefaults(SpeechToTextOptions options)
    {
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.ModelId);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.SpeechLanguage);
        Assert.Null(options.SpeechSampleRate);
    }

    [Fact]
    public void Merge_MembersCopiedOver()
    {
        using TestSpeechToTextClient c1 = new();
        using TestSpeechToTextClient c2 = new();
        using TestSpeechToTextClient c3 = new();

        SpeechToTextOptions options = new();
        AssertDefaults(options);

        options.Merge(null);
        AssertDefaults(options);

        options.Merge(new SpeechToTextOptions());
        AssertDefaults(options);

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key"] = "value" },
            SpeechLanguage = "en-US",
            SpeechSampleRate = 44100,
            TextLanguage = "fr-FR",
            ModelId = "modelId",
            RawRepresentationFactory = c => c == c1 ? new FormatException() : null,
        });

        Assert.Equal("en-US", options.SpeechLanguage);
        Assert.Equal("fr-FR", options.TextLanguage);
        Assert.Equal(44100, options.SpeechSampleRate);
        Assert.Equal("modelId", options.ModelId);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Single(options.AdditionalProperties);
        Assert.True(options.AdditionalProperties.ContainsKey("key"));
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(c1));
        Assert.Null(options.RawRepresentationFactory?.Invoke(c2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(c3));

        options.Merge(new()
        {
            AdditionalProperties = new() { ["key2"] = "value2" },
            SpeechLanguage = "fr-FR",
            SpeechSampleRate = 12345,
            TextLanguage = "de-DE",
            ModelId = "modelId2",
            RawRepresentationFactory = c => c == c2 ? new ArgumentException() : null,
        });

        Assert.Equal("en-US", options.SpeechLanguage);
        Assert.Equal("fr-FR", options.TextLanguage);
        Assert.Equal(44100, options.SpeechSampleRate);
        Assert.Equal("modelId", options.ModelId);
        Assert.NotNull(options.AdditionalProperties);
        Assert.Equal(2, options.AdditionalProperties.Count);
        Assert.True(options.AdditionalProperties.ContainsKey("key"));
        Assert.True(options.AdditionalProperties.ContainsKey("key2"));
        Assert.IsType<FormatException>(options.RawRepresentationFactory?.Invoke(c1));
        Assert.IsType<ArgumentException>(options.RawRepresentationFactory?.Invoke(c2));
        Assert.Null(options.RawRepresentationFactory?.Invoke(c3));
    }

    [Fact]
    public void Properties_Roundtrip()
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

        Assert.Equal("modelId", options.ModelId);
        Assert.Equal("en-US", options.SpeechLanguage);
        Assert.Equal(44100, options.SpeechSampleRate);
        Assert.Same(additionalProps, options.AdditionalProperties);

        SpeechToTextOptions clone = options.Clone();
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal("en-US", clone.SpeechLanguage);
        Assert.Equal(44100, clone.SpeechSampleRate);
        Assert.Equal(additionalProps, clone.AdditionalProperties);
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
}
