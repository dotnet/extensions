// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextResponseTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("message", () => new SpeechToTextResponse((SpeechToTextMessage)null!));
        Assert.Throws<ArgumentNullException>("choices", () => new SpeechToTextResponse((IList<SpeechToTextMessage>)null!));
    }

    [Fact]
    public void Constructor_Choice_Roundtrips()
    {
        SpeechToTextMessage choice = new();
        SpeechToTextResponse completion = new(choice);

        // The choice property returns the first (and only) choice.
        Assert.Same(choice, completion.Message);
        Assert.Same(choice, Assert.Single(completion.Choices));
    }

    [Fact]
    public void Constructor_Choices_Roundtrips()
    {
        List<SpeechToTextMessage> choices =
        [
            new SpeechToTextMessage(),
            new SpeechToTextMessage(),
            new SpeechToTextMessage(),
        ];

        SpeechToTextResponse completion = new(choices);
        Assert.Same(choices, completion.Choices);
        Assert.Equal(3, choices.Count);
    }

    [Fact]
    public void Response_EmptyChoices_Throws()
    {
        SpeechToTextResponse completion = new([]);
        Assert.Empty(completion.Choices);
        Assert.Throws<InvalidOperationException>(() => completion.Message);
    }

    [Fact]
    public void Response_SingleChoice_Returned()
    {
        SpeechToTextMessage choice = new();
        SpeechToTextResponse completion = new([choice]);
        Assert.Same(choice, completion.Message);
        Assert.Same(choice, completion.Choices[0]);
    }

    [Fact]
    public void Response_MultipleChoices_ReturnsFirst()
    {
        SpeechToTextMessage first = new();
        SpeechToTextResponse completion = new([
            first,
            new SpeechToTextMessage(),
        ]);
        Assert.Same(first, completion.Message);
        Assert.Same(first, completion.Choices[0]);
    }

    [Fact]
    public void Choices_SetNull_Throws()
    {
        SpeechToTextResponse completion = new([]);
        Assert.Throws<ArgumentNullException>("value", () => completion.Choices = null!);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        SpeechToTextResponse completion = new([]);
        Assert.Null(completion.ResponseId);
        completion.ResponseId = "id";
        Assert.Equal("id", completion.ResponseId);

        Assert.Null(completion.ModelId);
        completion.ModelId = "modelId";
        Assert.Equal("modelId", completion.ModelId);

        Assert.Null(completion.RawRepresentation);
        object raw = new();
        completion.RawRepresentation = raw;
        Assert.Same(raw, completion.RawRepresentation);

        Assert.Null(completion.AdditionalProperties);
        AdditionalPropertiesDictionary additionalProps = [];
        completion.AdditionalProperties = additionalProps;
        Assert.Same(additionalProps, completion.AdditionalProperties);

        List<SpeechToTextMessage> newChoices = [new SpeechToTextMessage(), new SpeechToTextMessage()];
        completion.Choices = newChoices;
        Assert.Same(newChoices, completion.Choices);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        SpeechToTextResponse original = new(
        [
            new SpeechToTextMessage("Choice1"),
            new SpeechToTextMessage("Choice2"),
            new SpeechToTextMessage("Choice3"),
            new SpeechToTextMessage("Choice4"),
        ])
        {
            ResponseId = "id",
            ModelId = "modelId",
            RawRepresentation = new(),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.SpeechToTextResponse);

        SpeechToTextResponse? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.SpeechToTextResponse);

        Assert.NotNull(result);
        Assert.Equal(4, result.Choices.Count);

        for (int i = 0; i < original.Choices.Count; i++)
        {
            Assert.Equal($"Choice{i + 1}", result.Choices[i].Text);
        }

        Assert.Equal("id", result.ResponseId);
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
        SpeechToTextResponse completion = new(
        [
            new SpeechToTextMessage("This is a test." + Environment.NewLine + "It's multiple lines.")
        ]);
        Assert.Equal(completion.Choices[0].Text, completion.ToString());
    }

    [Fact]
    public void ToString_MultipleChoices_OutputsAllChoicesWithPrefix()
    {
        SpeechToTextResponse completion = new(
        [
            new SpeechToTextMessage("This is a test." + Environment.NewLine + "It's multiple lines."),
            new SpeechToTextMessage("So is" + Environment.NewLine + " this."),
            new SpeechToTextMessage("And this."),
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
    public void ToSpeechToTextResponseUpdates_SingleChoice_ReturnsExpectedUpdates()
    {
        // Arrange: create a completion with one choice.
        SpeechToTextMessage choice = new("Text")
        {
            InputIndex = 0,
            StartTime = TimeSpan.FromSeconds(1),
            EndTime = TimeSpan.FromSeconds(2)
        };

        SpeechToTextResponse completion = new(choice)
        {
            ResponseId = "12345",
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        // Act: convert to streaming updates.
        SpeechToTextResponseUpdate[] updates = completion.ToSpeechToTextResponseUpdates();

        // Filter out any null entries (if any).
        SpeechToTextResponseUpdate[] nonNullUpdates = updates.Where(u => u is not null).ToArray();

        // Our implementation creates one update per choice plus an extra update if AdditionalProperties exists.
        Assert.Equal(2, nonNullUpdates.Length);

        SpeechToTextResponseUpdate update0 = nonNullUpdates[0];
        Assert.Equal("12345", update0.ResponseId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdated, update0.Kind);
        Assert.Equal(choice.Text, update0.Text);
        Assert.Equal(choice.InputIndex, update0.InputIndex);
        Assert.Equal(choice.StartTime, update0.StartTime);
        Assert.Equal(choice.EndTime, update0.EndTime);

        SpeechToTextResponseUpdate updateExtra = nonNullUpdates[1];

        // The extra update carries the AdditionalProperties from the completion.
        Assert.Null(updateExtra.Text);
        Assert.Equal("value1", updateExtra.AdditionalProperties?["key1"]);
        Assert.Equal(42, updateExtra.AdditionalProperties?["key2"]);
    }

    [Fact]
    public void ToSpeechToTextResponseUpdates_MultiChoice_ReturnsExpectedUpdates()
    {
        // Arrange: create two choices.
        SpeechToTextMessage choice1 = new(
        [
            new TextContent("Hello, "),
            new DataContent("data:image/png;base64,AQIDBA==", mediaType: "image/png"),
            new TextContent("world!")
        ])
        {
            AdditionalProperties = new() { ["choice1Key"] = "choice1Value" },
            InputIndex = 0
        };

        SpeechToTextMessage choice2 = new(
        [
            new FunctionCallContent("call123", "name"),
            new FunctionResultContent("call123", 42),
        ])
        {
            AdditionalProperties = new() { ["choice2Key"] = "choice2Value" },
            InputIndex = 1
        };

        SpeechToTextResponse completion = new([choice1, choice2])
        {
            ResponseId = "12345",
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        // Act: convert to streaming updates.
        SpeechToTextResponseUpdate[] updates = completion.ToSpeechToTextResponseUpdates();
        SpeechToTextResponseUpdate[] nonNullUpdates = updates.Where(u => u is not null).ToArray();

        // Two choices plus an extra update should yield 3 updates.
        Assert.Equal(3, nonNullUpdates.Length);

        // Validate update from first choice.
        SpeechToTextResponseUpdate update0 = nonNullUpdates[0];
        Assert.Equal("12345", update0.ResponseId);
        Assert.Equal("someModel", update0.ModelId);
        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdated, update0.Kind);
        Assert.Equal("Hello, ", Assert.IsType<TextContent>(update0.Contents[0]).Text);
        Assert.Equal("image/png", Assert.IsType<DataContent>(update0.Contents[1]).MediaType);
        Assert.Equal("world!", Assert.IsType<TextContent>(update0.Contents[2]).Text);
        Assert.Equal(choice1.InputIndex, update0.InputIndex);
        Assert.Equal("choice1Value", update0.AdditionalProperties?["choice1Key"]);

        // Validate update from second choice.
        SpeechToTextResponseUpdate update1 = nonNullUpdates[1];
        Assert.Equal("12345", update1.ResponseId);
        Assert.Equal("someModel", update1.ModelId);
        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdated, update1.Kind);

        // For choice2 (function call and result), we do not expect a concatenated text.
        Assert.True(update1.Contents.Count >= 2);
        Assert.IsType<FunctionCallContent>(update1.Contents[0]);
        Assert.IsType<FunctionResultContent>(update1.Contents[1]);
        Assert.Equal("choice2Value", update1.AdditionalProperties?["choice2Key"]);

        // Validate the extra update.
        SpeechToTextResponseUpdate updateExtra = nonNullUpdates[2];
        Assert.Null(updateExtra.Text);
        Assert.Equal("value1", updateExtra.AdditionalProperties?["key1"]);
        Assert.Equal(42, updateExtra.AdditionalProperties?["key2"]);
    }
}
