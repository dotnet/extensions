// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        SpeechToTextOptions options = new();
        Assert.Null(options.ResponseId);
        Assert.Null(options.ModelId);
        Assert.Null(options.SpeechLanguage);
        Assert.Null(options.SpeechSampleRate);
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.Prompt);

        SpeechToTextOptions clone = options.Clone();
        Assert.Null(clone.ResponseId);
        Assert.Null(clone.ModelId);
        Assert.Null(clone.SpeechLanguage);
        Assert.Null(clone.SpeechSampleRate);
        Assert.Null(clone.AdditionalProperties);
        Assert.Null(clone.Prompt);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        SpeechToTextOptions options = new();

        AdditionalPropertiesDictionary additionalProps = new()
        {
            ["key"] = "value",
        };

        options.ResponseId = "completionId";
        options.ModelId = "modelId";
        options.SpeechLanguage = "en-US";
        options.SpeechSampleRate = 44100;
        options.Prompt = "prompt";
        options.AdditionalProperties = additionalProps;

        Assert.Equal("completionId", options.ResponseId);
        Assert.Equal("modelId", options.ModelId);
        Assert.Equal("en-US", options.SpeechLanguage);
        Assert.Equal("prompt", options.Prompt);
        Assert.Equal(44100, options.SpeechSampleRate);
        Assert.Same(additionalProps, options.AdditionalProperties);

        SpeechToTextOptions clone = options.Clone();
        Assert.Equal("completionId", clone.ResponseId);
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal("en-US", clone.SpeechLanguage);
        Assert.Equal("prompt", clone.Prompt);
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

        options.ResponseId = "completionId";
        options.ModelId = "modelId";
        options.SpeechLanguage = "en-US";
        options.SpeechSampleRate = 44100;
        options.Prompt = "prompt";
        options.AdditionalProperties = additionalProps;

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.SpeechToTextOptions);

        SpeechToTextOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.SpeechToTextOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("completionId", deserialized.ResponseId);
        Assert.Equal("modelId", deserialized.ModelId);
        Assert.Equal("en-US", deserialized.SpeechLanguage);
        Assert.Equal(44100, deserialized.SpeechSampleRate);
        Assert.Equal("prompt", deserialized.Prompt);

        Assert.NotNull(deserialized.AdditionalProperties);
        Assert.Single(deserialized.AdditionalProperties);
        Assert.True(deserialized.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }
}
