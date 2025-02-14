// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class StreamingAudioTranscriptionUpdateTests
{
    [Fact]
    public void Constructor_PropsDefaulted()
    {
        AudioTranscriptionResponseUpdate update = new();

        Assert.Equal(AudioTranscriptionResponseUpdateKind.Transcribing, update.Kind);
        Assert.Null(update.Text);
        Assert.Empty(update.Contents);
        Assert.Null(update.TranscriptionId);
        Assert.Equal(0, update.ChoiceIndex);
        Assert.Null(update.StartTime);
        Assert.Null(update.EndTime);
        Assert.Equal(string.Empty, update.ToString());
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        AudioTranscriptionResponseUpdate update = new()
        {
            InputIndex = 5,
            ChoiceIndex = 42,
            Kind = new AudioTranscriptionResponseUpdateKind("custom"),
        };

        Assert.Equal(5, update.InputIndex);
        Assert.Equal(42, update.ChoiceIndex);
        Assert.Equal("custom", update.Kind.Value);

        // Test the computed Text property
        Assert.Null(update.Text);
        update.Text = "sample text";
        Assert.Equal("sample text", update.Text);

        // Contents: assigning a new list then resetting to null should yield an empty list.
        List<AIContent> newList = new();
        newList.Add(new TextContent("content1"));
        update.Contents = newList;
        Assert.Same(newList, update.Contents);
        update.Contents = null;
        Assert.NotNull(update.Contents);
        Assert.Empty(update.Contents);

        update.TranscriptionId = "comp123";
        Assert.Equal("comp123", update.TranscriptionId);

        update.StartTime = TimeSpan.FromSeconds(10);
        update.EndTime = TimeSpan.FromSeconds(20);
        Assert.Equal(TimeSpan.FromSeconds(10), update.StartTime);
        Assert.Equal(TimeSpan.FromSeconds(20), update.EndTime);
    }

    [Fact]
    public void Text_GetSet_UsesFirstTextContent()
    {
        AudioTranscriptionResponseUpdate update = new(
        [
            new DataContent("http://localhost/audio", "application/octet-stream"),
            new DataContent("http://localhost/image", "application/octet-stream"),
            new FunctionCallContent("callId1", "fc1"),
            new TextContent("text-1"),
            new TextContent("text-2"),
            new FunctionResultContent("callId1", "result"),
        ]);

        // The getter returns the text of the first TextContent (which is at index 3).
        TextContent textContent = Assert.IsType<TextContent>(update.Contents[3]);
        Assert.Equal("text-1", textContent.Text);
        Assert.Equal("text-1", update.Text);

        // Assume the ToString concatenates the text of all TextContent items.
        Assert.Equal("text-1text-2", update.ToString());

        update.Text = "text-3";
        Assert.Equal("text-3", update.Text);

        // The setter should update the first TextContent item.
        Assert.Same(textContent, update.Contents[3]);
        Assert.Equal("text-3text-2", update.ToString());
    }

    [Fact]
    public void Text_Set_AddsTextMessageToEmptyList()
    {
        AudioTranscriptionResponseUpdate update = new();
        Assert.Empty(update.Contents);

        update.Text = "text-1";
        Assert.Equal("text-1", update.Text);

        Assert.Single(update.Contents);
        TextContent textContent = Assert.IsType<TextContent>(update.Contents[0]);
        Assert.Equal("text-1", textContent.Text);
    }

    [Fact]
    public void Text_Set_AddsTextMessageToListWithNoText()
    {
        AudioTranscriptionResponseUpdate update = new(
        [
            new DataContent("http://localhost/audio", "application/octet-stream"),
            new DataContent("http://localhost/image", "application/octet-stream"),
            new FunctionCallContent("callId1", "fc1"),
        ]);
        Assert.Equal(3, update.Contents.Count);

        update.Text = "text-1";
        Assert.Equal("text-1", update.Text);
        Assert.Equal(4, update.Contents.Count);

        update.Text = "text-2";
        Assert.Equal("text-2", update.Text);
        Assert.Equal(4, update.Contents.Count);

        update.Contents.RemoveAt(3);
        Assert.Equal(3, update.Contents.Count);

        update.Text = "text-3";
        Assert.Equal("text-3", update.Text);
        Assert.Equal(4, update.Contents.Count);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        AudioTranscriptionResponseUpdate original = new()
        {
            InputIndex = 7,
            ChoiceIndex = 3,
            Kind = new AudioTranscriptionResponseUpdateKind("transcribed"),
            TranscriptionId = "id123",
            StartTime = TimeSpan.FromSeconds(5),
            EndTime = TimeSpan.FromSeconds(10),
            Contents = new List<AIContent>
            {
                new TextContent("text-1"),
                new DataContent("http://localhost/image", "application/octet-stream")
            }
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.AudioTranscriptionResponseUpdate);
        AudioTranscriptionResponseUpdate? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.AudioTranscriptionResponseUpdate);
        Assert.NotNull(result);

        Assert.Equal(original.InputIndex, result.InputIndex);
        Assert.Equal(original.ChoiceIndex, result.ChoiceIndex);
        Assert.Equal(original.Kind, result.Kind);
        Assert.Equal(original.TranscriptionId, result.TranscriptionId);
        Assert.Equal(original.StartTime, result.StartTime);
        Assert.Equal(original.EndTime, result.EndTime);
        Assert.Equal(original.Contents.Count, result.Contents.Count);
        for (int i = 0; i < original.Contents.Count; i++)
        {
            // Compare via string conversion.
            Assert.Equal(original.Contents[i].ToString(), result.Contents[i].ToString());
        }
    }
}
