// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionCompletionTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("audioTranscription", () => new AudioTranscriptionResponse((AudioTranscription)null!));
        Assert.Throws<ArgumentNullException>("choices", () => new AudioTranscriptionResponse((IList<AudioTranscription>)null!));
    }

    [Fact]
    public void Constructor_Choice_Roundtrips()
    {
        AudioTranscription choice = new();
        AudioTranscriptionResponse completion = new(choice);

        // The choice property returns the first (and only) choice.
        Assert.Same(choice, completion.AudioTranscription);
        Assert.Same(choice, Assert.Single(completion.Choices));
    }

    [Fact]
    public void Constructor_Choices_Roundtrips()
    {
        List<AudioTranscription> choices =
        [
            new AudioTranscription(),
            new AudioTranscription(),
            new AudioTranscription(),
        ];

        AudioTranscriptionResponse completion = new(choices);
        Assert.Same(choices, completion.Choices);
        Assert.Equal(3, choices.Count);
    }

    [Fact]
    public void Transcription_EmptyChoices_Throws()
    {
        AudioTranscriptionResponse completion = new([]);
        Assert.Empty(completion.Choices);
        Assert.Throws<InvalidOperationException>(() => completion.AudioTranscription);
    }

    [Fact]
    public void Transcription_SingleChoice_Returned()
    {
        AudioTranscription choice = new();
        AudioTranscriptionResponse completion = new([choice]);
        Assert.Same(choice, completion.AudioTranscription);
        Assert.Same(choice, completion.Choices[0]);
    }

    [Fact]
    public void Transcription_MultipleChoices_ReturnsFirst()
    {
        AudioTranscription first = new();
        AudioTranscriptionResponse completion = new([
            first,
            new AudioTranscription(),
        ]);
        Assert.Same(first, completion.AudioTranscription);
        Assert.Same(first, completion.Choices[0]);
    }

    [Fact]
    public void Choices_SetNull_Throws()
    {
        AudioTranscriptionResponse completion = new([]);
        Assert.Throws<ArgumentNullException>("value", () => completion.Choices = null!);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        AudioTranscriptionResponse completion = new([]);
        Assert.Null(completion.TranscriptionId);
        completion.TranscriptionId = "id";
        Assert.Equal("id", completion.TranscriptionId);

        Assert.Null(completion.ModelId);
        completion.ModelId = "modelId";
        Assert.Equal("modelId", completion.ModelId);

        // AudioTranscriptionResponse does not support CreatedAt, FinishReason or Usage.
        Assert.Null(completion.RawRepresentation);
        object raw = new();
        completion.RawRepresentation = raw;
        Assert.Same(raw, completion.RawRepresentation);

        Assert.Null(completion.AdditionalProperties);
        AdditionalPropertiesDictionary additionalProps = [];
        completion.AdditionalProperties = additionalProps;
        Assert.Same(additionalProps, completion.AdditionalProperties);

        List<AudioTranscription> newChoices = [new AudioTranscription(), new AudioTranscription()];
        completion.Choices = newChoices;
        Assert.Same(newChoices, completion.Choices);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        AudioTranscriptionResponse original = new(
        [
            new AudioTranscription("Choice1"),
            new AudioTranscription("Choice2"),
            new AudioTranscription("Choice3"),
            new AudioTranscription("Choice4"),
        ])
        {
            TranscriptionId = "id",
            ModelId = "modelId",
            RawRepresentation = new(),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.AudioTranscriptionResponse);

        AudioTranscriptionResponse? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.AudioTranscriptionResponse);

        Assert.NotNull(result);
        Assert.Equal(4, result.Choices.Count);

        for (int i = 0; i < original.Choices.Count; i++)
        {
            Assert.Equal($"Choice{i + 1}", result.Choices[i].Text);
        }

        Assert.Equal("id", result.TranscriptionId);
        Assert.Equal("modelId", result.ModelId);

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }

    [Fact]
    public void ToString_SingleChoice_OutputsChoiceText()
    {
        AudioTranscriptionResponse completion = new(
        [
            new AudioTranscription("This is a test." + Environment.NewLine + "It's multiple lines.")
        ]);
        Assert.Equal(completion.Choices[0].Text, completion.ToString());
    }

    [Fact]
    public void ToString_MultipleChoices_OutputsAllChoicesWithPrefix()
    {
        AudioTranscriptionResponse completion = new(
        [
            new AudioTranscription("This is a test." + Environment.NewLine + "It's multiple lines."),
            new AudioTranscription("So is" + Environment.NewLine + " this."),
            new AudioTranscription("And this."),
        ]);

        StringBuilder sb = new();

        for (int i = 0; i < completion.Choices.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine().AppendLine();
            }

            sb.Append("Choice ").Append(i).AppendLine(":").Append(completion.Choices[i].ToString());
        }

        string expected = sb.ToString();
        Assert.Equal(expected, completion.ToString());
    }

    [Fact]
    public void ToStreamingAudioTranscriptionUpdates_SingleChoice_ReturnsExpectedUpdates()
    {
        // Arrange: create a completion with one choice.
        AudioTranscription choice = new("Text")
        {
            InputIndex = 0,
            StartTime = TimeSpan.FromSeconds(1),
            EndTime = TimeSpan.FromSeconds(2)
        };

        AudioTranscriptionResponse completion = new(choice)
        {
            TranscriptionId = "12345",
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        // Act: convert to streaming updates.
        AudioTranscriptionResponseUpdate[] updates = completion.ToStreamingAudioTranscriptionUpdates();

        // Filter out any null entries (if any).
        AudioTranscriptionResponseUpdate[] nonNullUpdates = updates.Where(u => u is not null).ToArray();

        // Our implementation creates one update per choice plus an extra update if AdditionalProperties exists.
        Assert.Equal(2, nonNullUpdates.Length);

        AudioTranscriptionResponseUpdate update0 = nonNullUpdates[0];
        Assert.Equal("12345", update0.TranscriptionId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(AudioTranscriptionResponseUpdateKind.Transcribed, update0.Kind);
        Assert.Equal(choice.Text, update0.Text);
        Assert.Equal(choice.InputIndex, update0.InputIndex);
        Assert.Equal(choice.StartTime, update0.StartTime);
        Assert.Equal(choice.EndTime, update0.EndTime);

        AudioTranscriptionResponseUpdate updateExtra = nonNullUpdates[1];

        // The extra update carries the AdditionalProperties from the completion.
        Assert.Null(updateExtra.Text);
        Assert.Equal("value1", updateExtra.AdditionalProperties?["key1"]);
        Assert.Equal(42, updateExtra.AdditionalProperties?["key2"]);
    }

    [Fact]
    public void ToStreamingAudioTranscriptionUpdates_MultiChoice_ReturnsExpectedUpdates()
    {
        // Arrange: create two choices.
        AudioTranscription choice1 = new(
        [
            new TextContent("Hello, "),
            new DataContent("http://localhost/image.png", mediaType: "image/png"),
            new TextContent("world!")
        ])
        {
            AdditionalProperties = new() { ["choice1Key"] = "choice1Value" },
            InputIndex = 0
        };

        AudioTranscription choice2 = new(
        [
            new FunctionCallContent("call123", "name"),
            new FunctionResultContent("call123", 42),
        ])
        {
            AdditionalProperties = new() { ["choice2Key"] = "choice2Value" },
            InputIndex = 1
        };

        AudioTranscriptionResponse completion = new([choice1, choice2])
        {
            TranscriptionId = "12345",
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        // Act: convert to streaming updates.
        AudioTranscriptionResponseUpdate[] updates = completion.ToStreamingAudioTranscriptionUpdates();
        AudioTranscriptionResponseUpdate[] nonNullUpdates = updates.Where(u => u is not null).ToArray();

        // Two choices plus an extra update should yield 3 updates.
        Assert.Equal(3, nonNullUpdates.Length);

        // Validate update from first choice.
        AudioTranscriptionResponseUpdate update0 = nonNullUpdates[0];
        Assert.Equal("12345", update0.TranscriptionId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(AudioTranscriptionResponseUpdateKind.Transcribed, update0.Kind);
        Assert.Equal("Hello, ", Assert.IsType<TextContent>(update0.Contents[0]).Text);
        Assert.Equal("image/png", Assert.IsType<DataContent>(update0.Contents[1]).MediaType);
        Assert.Equal("world!", Assert.IsType<TextContent>(update0.Contents[2]).Text);
        Assert.Equal(choice1.InputIndex, update0.InputIndex);
        Assert.Equal("choice1Value", update0.AdditionalProperties?["choice1Key"]);

        // Validate update from second choice.
        AudioTranscriptionResponseUpdate update1 = nonNullUpdates[1];
        Assert.Equal("12345", update1.TranscriptionId);
        Assert.Equal("someModel", update1.ModelId);
        Assert.Equal(AudioTranscriptionResponseUpdateKind.Transcribed, update1.Kind);

        // For choice2 (function call and result), we do not expect a concatenated text.
        Assert.True(update1.Contents.Count >= 2);
        Assert.IsType<FunctionCallContent>(update1.Contents[0]);
        Assert.IsType<FunctionResultContent>(update1.Contents[1]);
        Assert.Equal("choice2Value", update1.AdditionalProperties?["choice2Key"]);

        // Validate the extra update.
        AudioTranscriptionResponseUpdate updateExtra = nonNullUpdates[2];
        Assert.Null(updateExtra.Text);
        Assert.Equal("value1", updateExtra.AdditionalProperties?["key1"]);
        Assert.Equal(42, updateExtra.AdditionalProperties?["key2"]);
    }
}
