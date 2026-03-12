// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechResponseUpdateTests
{
    [Fact]
    public void Constructor_PropsDefaulted()
    {
        TextToSpeechResponseUpdate update = new();

        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdating, update.Kind);
        Assert.Empty(update.Contents);
        Assert.Null(update.ResponseId);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        TextToSpeechResponseUpdate update = new()
        {
            Kind = new TextToSpeechResponseUpdateKind("custom"),
        };

        Assert.Equal("custom", update.Kind.Value);

        // Contents: assigning a new list then resetting to null should yield an empty list.
        List<AIContent> newList = [new DataContent(new byte[] { 1 }, "audio/mpeg")];
        update.Contents = newList;
        Assert.Same(newList, update.Contents);
        update.Contents = null;
        Assert.NotNull(update.Contents);
        Assert.Empty(update.Contents);

        update.ResponseId = "comp123";
        Assert.Equal("comp123", update.ResponseId);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        TextToSpeechResponseUpdate original = new()
        {
            Kind = new TextToSpeechResponseUpdateKind("audiogenerated"),
            ResponseId = "id123",
            RawRepresentation = new Dictionary<string, object?> { ["value"] = 42 },
            Contents =
            [
                new DataContent(new byte[] { 1, 2, 3 }, "audio/mpeg"),
            ]
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.TextToSpeechResponseUpdate);
        TextToSpeechResponseUpdate? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToSpeechResponseUpdate);
        Assert.NotNull(result);

        Assert.Equal(original.Kind, result.Kind);
        Assert.Equal(original.ResponseId, result.ResponseId);
        JsonElement rawRepresentation = Assert.IsType<JsonElement>(result.RawRepresentation);
        Assert.Equal(42, rawRepresentation.GetProperty("value").GetInt32());
        Assert.Equal(original.Contents.Count, result.Contents.Count);
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "kind": "audioupdated",
              "responseId": "resp1",
              "modelId": "tts-1",
              "contents": [
                {
                  "$type": "data",
                  "uri": "data:audio/mpeg;base64,AQID"
                }
              ],
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        TextToSpeechResponseUpdate? result = JsonSerializer.Deserialize<TextToSpeechResponseUpdate>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdated, result.Kind);
        Assert.Equal("resp1", result.ResponseId);
        Assert.Equal("tts-1", result.ModelId);
        Assert.Single(result.Contents);
        var dataContent = Assert.IsType<DataContent>(result.Contents[0]);
        Assert.Equal("audio/mpeg", dataContent.MediaType);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal("val", result.AdditionalProperties["key"]?.ToString());
    }
}
