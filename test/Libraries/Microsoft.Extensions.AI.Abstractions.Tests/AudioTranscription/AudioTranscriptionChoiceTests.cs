// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AudioTranscriptionChoiceTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        AudioTranscription choice = new();
        Assert.Empty(choice.Contents);
        Assert.Null(choice.Text);
        Assert.NotNull(choice.Contents);
        Assert.Same(choice.Contents, choice.Contents);
        Assert.Empty(choice.Contents);
        Assert.Null(choice.RawRepresentation);
        Assert.Null(choice.AdditionalProperties);
        Assert.Null(choice.StartTime);
        Assert.Null(choice.EndTime);
        Assert.Equal(string.Empty, choice.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text")]
    public void Constructor_String_PropsRoundtrip(string? text)
    {
        AudioTranscription choice = new(text);

        Assert.Same(choice.Contents, choice.Contents);
        if (text is null)
        {
            Assert.Empty(choice.Contents);
        }
        else
        {
            Assert.Single(choice.Contents);
            TextContent tc = Assert.IsType<TextContent>(choice.Contents[0]);
            Assert.Equal(text, tc.Text);
        }

        Assert.Null(choice.RawRepresentation);
        Assert.Null(choice.AdditionalProperties);
        Assert.Equal(text ?? string.Empty, choice.ToString());
    }

    [Fact]
    public void Constructor_List_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("contents", () => new AudioTranscription((IList<AIContent>)null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_List_PropsRoundtrip(int choiceCount)
    {
        List<AIContent> content = [];
        for (int i = 0; i < choiceCount; i++)
        {
            content.Add(new TextContent($"text-{i}"));
        }

        AudioTranscription choice = new(content);

        Assert.Same(choice.Contents, choice.Contents);
        if (choiceCount == 0)
        {
            Assert.Empty(choice.Contents);
            Assert.Null(choice.Text);
        }
        else
        {
            Assert.Equal(choiceCount, choice.Contents.Count);
            for (int i = 0; i < choiceCount; i++)
            {
                TextContent tc = Assert.IsType<TextContent>(choice.Contents[i]);
                Assert.Equal($"text-{i}", tc.Text);
            }

            Assert.Equal("text-0", choice.Text);
            Assert.Equal(string.Concat(Enumerable.Range(0, choiceCount).Select(i => $"text-{i}")), choice.ToString());
        }

        Assert.Null(choice.RawRepresentation);
        Assert.Null(choice.AdditionalProperties);
    }

    [Fact]
    public void Text_GetSet_UsesFirstTextContent()
    {
        AudioTranscription choice = new(
        [
            new DataContent("http://localhost/audio"),
            new DataContent("http://localhost/image"),
            new FunctionCallContent("callId1", "fc1"),
            new TextContent("text-1"),
            new TextContent("text-2"),
            new FunctionResultContent("callId1", "result"),
        ]);

        TextContent textContent = Assert.IsType<TextContent>(choice.Contents[3]);
        Assert.Equal("text-1", textContent.Text);
        Assert.Equal("text-1", choice.Text);
        Assert.Equal("text-1text-2", choice.ToString());

        choice.Text = "text-3";
        Assert.Equal("text-3", choice.Text);
        Assert.Equal("text-3", choice.Text);
        Assert.Same(textContent, choice.Contents[3]);
        Assert.Equal("text-3text-2", choice.ToString());
    }

    [Fact]
    public void Text_Set_AddsTextToEmptyList()
    {
        AudioTranscription choice = new([]);
        Assert.Empty(choice.Contents);

        choice.Text = "text-1";
        Assert.Equal("text-1", choice.Text);

        Assert.Single(choice.Contents);
        TextContent textContent = Assert.IsType<TextContent>(choice.Contents[0]);
        Assert.Equal("text-1", textContent.Text);
    }

    [Fact]
    public void Text_Set_AddsTextToListWithNoText()
    {
        AudioTranscription choice = new(
        [
            new DataContent("http://localhost/audio"),
            new DataContent("http://localhost/image"),
            new FunctionCallContent("callId1", "fc1"),
        ]);
        Assert.Equal(3, choice.Contents.Count);

        choice.Text = "text-1";
        Assert.Equal("text-1", choice.Text);
        Assert.Equal(4, choice.Contents.Count);

        choice.Text = "text-2";
        Assert.Equal("text-2", choice.Text);
        Assert.Equal(4, choice.Contents.Count);

        choice.Contents.RemoveAt(3);
        Assert.Equal(3, choice.Contents.Count);

        choice.Text = "text-3";
        Assert.Equal("text-3", choice.Text);
        Assert.Equal(4, choice.Contents.Count);
    }

    [Fact]
    public void Contents_InitializesToList()
    {
        // This is an implementation detail, but if this test starts failing, we need to ensure
        // tests are in place for whatever possibly-custom implementation of IList is being used.
        Assert.IsType<List<AIContent>>(new AudioTranscription().Contents);
    }

    [Fact]
    public void Contents_Roundtrips()
    {
        AudioTranscription choice = new();
        Assert.Empty(choice.Contents);

        List<AIContent> contents = [];
        choice.Contents = contents;

        Assert.Same(contents, choice.Contents);

        choice.Contents = contents;
        Assert.Same(contents, choice.Contents);

        choice.Contents = null;
        Assert.NotNull(choice.Contents);
        Assert.NotSame(contents, choice.Contents);
        Assert.Empty(choice.Contents);
    }

    [Fact]
    public void RawRepresentation_Roundtrips()
    {
        AudioTranscription choice = new();
        Assert.Null(choice.RawRepresentation);

        object raw = new();

        choice.RawRepresentation = raw;
        Assert.Same(raw, choice.RawRepresentation);

        // Ensure the idempotency of setting the same value
        choice.RawRepresentation = raw;
        Assert.Same(raw, choice.RawRepresentation);

        choice.RawRepresentation = null;
        Assert.Null(choice.RawRepresentation);

        choice.RawRepresentation = raw;
        Assert.Same(raw, choice.RawRepresentation);
    }

    [Fact]
    public void AdditionalProperties_Roundtrips()
    {
        AudioTranscription choice = new();
        Assert.Null(choice.RawRepresentation);

        AdditionalPropertiesDictionary props = [];

        choice.AdditionalProperties = props;
        Assert.Same(props, choice.AdditionalProperties);

        // Ensure the idempotency of setting the same value
        choice.AdditionalProperties = props;
        Assert.Same(props, choice.AdditionalProperties);

        choice.AdditionalProperties = null;
        Assert.Null(choice.AdditionalProperties);

        choice.AdditionalProperties = props;
        Assert.Same(props, choice.AdditionalProperties);
    }

    [Fact]
    public void StartTime_Roundtrips()
    {
        AudioTranscription choice = new();
        Assert.Null(choice.StartTime);

        TimeSpan startTime = TimeSpan.FromSeconds(10);
        choice.StartTime = startTime;
        Assert.Equal(startTime, choice.StartTime);

        choice.StartTime = null;
        Assert.Null(choice.StartTime);
    }

    [Fact]
    public void EndTime_Roundtrips()
    {
        AudioTranscription choice = new();
        Assert.Null(choice.EndTime);

        TimeSpan endTime = TimeSpan.FromSeconds(20);
        choice.EndTime = endTime;
        Assert.Equal(endTime, choice.EndTime);

        choice.EndTime = null;
        Assert.Null(choice.EndTime);
    }

    [Fact]
    public void ItCanBeSerializeAndDeserialized()
    {
        // Arrange
        IList<AIContent> items =
        [
            new TextContent("content-1")
            {
                AdditionalProperties = new() { ["metadata-key-1"] = "metadata-value-1" }
            },
            new DataContent(new Uri("https://fake-random-test-host:123"), "mime-type/2")
            {
                AdditionalProperties = new() { ["metadata-key-2"] = "metadata-value-2" }
            },
            new DataContent(new BinaryData(new[] { 1, 2, 3 }, options: TestJsonSerializerContext.Default.Options), "mime-type/3")
            {
                AdditionalProperties = new() { ["metadata-key-3"] = "metadata-value-3" }
            },
            new TextContent("content-4")
            {
                AdditionalProperties = new() { ["metadata-key-4"] = "metadata-value-4" }
            },
            new FunctionCallContent("function-id", "plugin-name-function-name", new Dictionary<string, object?> { ["parameter"] = "argument" }),
            new FunctionResultContent("function-id", "function-result"),
        ];

        // Act
        var audioTranscriptionChoiceJson = JsonSerializer.Serialize(new AudioTranscription(contents: items)
        {
            Text = "content-1-override", // Override the content of the first text content item that has the "content-1" content
            AdditionalProperties = new() { ["choice-metadata-key-1"] = "choice-metadata-value-1" },
            StartTime = TimeSpan.FromSeconds(10),
            EndTime = TimeSpan.FromSeconds(20)
        }, TestJsonSerializerContext.Default.Options);

        var deserializedChoice = JsonSerializer.Deserialize<AudioTranscription>(audioTranscriptionChoiceJson, TestJsonSerializerContext.Default.Options)!;

        // Assert
        Assert.NotNull(deserializedChoice.AdditionalProperties);
        Assert.Single(deserializedChoice.AdditionalProperties);
        Assert.Equal("choice-metadata-value-1", deserializedChoice.AdditionalProperties["choice-metadata-key-1"]?.ToString());
        Assert.Equal(TimeSpan.FromSeconds(10), deserializedChoice.StartTime);
        Assert.Equal(TimeSpan.FromSeconds(20), deserializedChoice.EndTime);

        Assert.NotNull(deserializedChoice.Contents);
        Assert.Equal(items.Count, deserializedChoice.Contents.Count);

        var textContent = deserializedChoice.Contents[0] as TextContent;
        Assert.NotNull(textContent);
        Assert.Equal("content-1-override", textContent.Text);
        Assert.NotNull(textContent.AdditionalProperties);
        Assert.Single(textContent.AdditionalProperties);
        Assert.Equal("metadata-value-1", textContent.AdditionalProperties["metadata-key-1"]?.ToString());

        var dataContent = deserializedChoice.Contents[1] as DataContent;
        Assert.NotNull(dataContent);
        Assert.Equal("https://fake-random-test-host:123/", dataContent.Uri);
        Assert.Equal("mime-type/2", dataContent.MediaType);
        Assert.NotNull(dataContent.AdditionalProperties);
        Assert.Single(dataContent.AdditionalProperties);
        Assert.Equal("metadata-value-2", dataContent.AdditionalProperties["metadata-key-2"]?.ToString());

        dataContent = deserializedChoice.Contents[2] as DataContent;
        Assert.NotNull(dataContent);
        Assert.True(dataContent.Data!.Value.Span.SequenceEqual(new BinaryData(new[] { 1, 2, 3 }, TestJsonSerializerContext.Default.Options)));
        Assert.Equal("mime-type/3", dataContent.MediaType);
        Assert.NotNull(dataContent.AdditionalProperties);
        Assert.Single(dataContent.AdditionalProperties);
        Assert.Equal("metadata-value-3", dataContent.AdditionalProperties["metadata-key-3"]?.ToString());

        textContent = deserializedChoice.Contents[3] as TextContent;
        Assert.NotNull(textContent);
        Assert.Equal("content-4", textContent.Text);
        Assert.NotNull(textContent.AdditionalProperties);
        Assert.Single(textContent.AdditionalProperties);
        Assert.Equal("metadata-value-4", textContent.AdditionalProperties["metadata-key-4"]?.ToString());

        var functionCallContent = deserializedChoice.Contents[4] as FunctionCallContent;
        Assert.NotNull(functionCallContent);
        Assert.Equal("plugin-name-function-name", functionCallContent.Name);
        Assert.Equal("function-id", functionCallContent.CallId);
        Assert.NotNull(functionCallContent.Arguments);
        Assert.Single(functionCallContent.Arguments);
        Assert.Equal("argument", functionCallContent.Arguments["parameter"]?.ToString());

        var functionResultContent = deserializedChoice.Contents[5] as FunctionResultContent;
        Assert.NotNull(functionResultContent);
        Assert.Equal("function-result", functionResultContent.Result?.ToString());
        Assert.Equal("function-id", functionResultContent.CallId);
    }
}
