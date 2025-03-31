// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextResponseUpdateTests
{
    [Fact]
    public void Constructor_PropsDefaulted()
    {
        SpeechToTextResponseUpdate update = new();

        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdating, update.Kind);
        Assert.Empty(update.Text);
        Assert.Empty(update.Contents);
        Assert.Null(update.ResponseId);
        Assert.Null(update.StartTime);
        Assert.Null(update.EndTime);
        Assert.Equal(string.Empty, update.ToString());
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        SpeechToTextResponseUpdate update = new()
        {
            Kind = new SpeechToTextResponseUpdateKind("custom"),
        };

        Assert.Equal("custom", update.Kind.Value);

        // Test the computed Text property
        Assert.Empty(update.Text);

        // Contents: assigning a new list then resetting to null should yield an empty list.
        List<AIContent> newList = new();
        newList.Add(new TextContent("content1"));
        update.Contents = newList;
        Assert.Same(newList, update.Contents);
        update.Contents = null;
        Assert.NotNull(update.Contents);
        Assert.Empty(update.Contents);

        update.ResponseId = "comp123";
        Assert.Equal("comp123", update.ResponseId);

        update.StartTime = TimeSpan.FromSeconds(10);
        update.EndTime = TimeSpan.FromSeconds(20);
        Assert.Equal(TimeSpan.FromSeconds(10), update.StartTime);
        Assert.Equal(TimeSpan.FromSeconds(20), update.EndTime);
    }

    [Fact]
    public void Text_Get_UsesFirstTextContent()
    {
        SpeechToTextResponseUpdate update = new(
        [
            new DataContent("data:audio/wav;base64,AQIDBA==", "application/octet-stream"),
            new DataContent("data:image/wav;base64,AQIDBA==", "application/octet-stream"),
            new FunctionCallContent("callId1", "fc1"),
            new TextContent("text-1"),
            new TextContent("text-2"),
            new FunctionResultContent("callId1", "result"),
        ]);

        // The getter returns the text of the first TextContent (which is at index 3).
        TextContent textContent = Assert.IsType<TextContent>(update.Contents[3]);
        Assert.Equal("text-1", textContent.Text);
        Assert.Equal("text-1text-2", update.Text);

        // Assume the ToString concatenates the text of all TextContent items.
        Assert.Equal("text-1text-2", update.ToString());

        // The setter should update the first TextContent item.
        Assert.Same(textContent, update.Contents[3]);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        SpeechToTextResponseUpdate original = new()
        {
            Kind = new SpeechToTextResponseUpdateKind("transcribed"),
            ResponseId = "id123",
            StartTime = TimeSpan.FromSeconds(5),
            EndTime = TimeSpan.FromSeconds(10),
            Contents = new List<AIContent>
            {
                new TextContent("text-1"),
                new DataContent("data:audio/wav;base64,AQIDBA==", "application/octet-stream")
            }
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.SpeechToTextResponseUpdate);
        SpeechToTextResponseUpdate? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.SpeechToTextResponseUpdate);
        Assert.NotNull(result);

        Assert.Equal(original.Kind, result.Kind);
        Assert.Equal(original.ResponseId, result.ResponseId);
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
