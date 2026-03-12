// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToSpeechResponseTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("contents", () => new TextToSpeechResponse(null!));
    }

    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        TextToSpeechResponse response = new();
        Assert.Empty(response.Contents);
        Assert.NotNull(response.Contents);
        Assert.Same(response.Contents, response.Contents);
        Assert.Empty(response.Contents);
        Assert.Null(response.RawRepresentation);
        Assert.Null(response.AdditionalProperties);
        Assert.Null(response.Usage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_List_PropsRoundtrip(int contentCount)
    {
        List<AIContent> content = [];
        for (int i = 0; i < contentCount; i++)
        {
            content.Add(new DataContent(new byte[] { (byte)i }, "audio/mpeg"));
        }

        TextToSpeechResponse response = new(content);

        Assert.Same(response.Contents, response.Contents);
        if (contentCount == 0)
        {
            Assert.Empty(response.Contents);
        }
        else
        {
            Assert.Equal(contentCount, response.Contents.Count);
            for (int i = 0; i < contentCount; i++)
            {
                DataContent dc = Assert.IsType<DataContent>(response.Contents[i]);
                Assert.Equal("audio/mpeg", dc.MediaType);
            }
        }
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        TextToSpeechResponse response = new();
        Assert.Null(response.ResponseId);
        response.ResponseId = "id";
        Assert.Equal("id", response.ResponseId);

        Assert.Null(response.ModelId);
        response.ModelId = "modelId";
        Assert.Equal("modelId", response.ModelId);

        Assert.Null(response.RawRepresentation);
        object raw = new();
        response.RawRepresentation = raw;
        Assert.Same(raw, response.RawRepresentation);

        Assert.Null(response.AdditionalProperties);
        AdditionalPropertiesDictionary additionalProps = [];
        response.AdditionalProperties = additionalProps;
        Assert.Same(additionalProps, response.AdditionalProperties);

        List<AIContent> newContents = [new DataContent(new byte[] { 1 }, "audio/mpeg"), new DataContent(new byte[] { 2 }, "audio/mpeg")];
        response.Contents = newContents;
        Assert.Same(newContents, response.Contents);

        Assert.Null(response.Usage);
        UsageDetails usageDetails = new();
        response.Usage = usageDetails;
        Assert.Same(usageDetails, response.Usage);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        TextToSpeechResponse original = new()
        {
            Contents =
            [
                new DataContent(new byte[] { 1, 2, 3 }, "audio/mpeg"),
            ],
            ResponseId = "id",
            ModelId = "modelId",
            RawRepresentation = new Dictionary<string, object?> { ["value"] = 42 },
            AdditionalProperties = new() { ["key"] = "value" },
            Usage = new() { InputTokenCount = 42, OutputTokenCount = 84, TotalTokenCount = 126 },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.TextToSpeechResponse);

        TextToSpeechResponse? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToSpeechResponse);

        Assert.NotNull(result);
        Assert.Single(result.Contents);

        Assert.Equal("id", result.ResponseId);
        Assert.Equal("modelId", result.ModelId);
        JsonElement rawRepresentation = Assert.IsType<JsonElement>(result.RawRepresentation);
        Assert.Equal(42, rawRepresentation.GetProperty("value").GetInt32());

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());

        Assert.NotNull(result.Usage);
        Assert.Equal(42, result.Usage.InputTokenCount);
        Assert.Equal(84, result.Usage.OutputTokenCount);
        Assert.Equal(126, result.Usage.TotalTokenCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToTextToSpeechResponseUpdates_ReturnsExpectedUpdate(bool withUsage)
    {
        // Arrange: create a response with contents
        TextToSpeechResponse response = new()
        {
            Contents =
            [
                new DataContent(new byte[] { 1, 2, 3 }, "audio/mpeg"),
                new DataContent(new byte[] { 4, 5, 6 }, "audio/wav"),
            ],
            ResponseId = "12345",
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
            Usage = withUsage ? new UsageDetails { InputTokenCount = 100, OutputTokenCount = 200, TotalTokenCount = 300 } : null
        };

        // Act: convert to streaming updates
        TextToSpeechResponseUpdate[] updates = response.ToTextToSpeechResponseUpdates();

        // Assert: should be a single update with all properties
        Assert.Single(updates);

        TextToSpeechResponseUpdate update = updates[0];
        Assert.Equal("12345", update.ResponseId);
        Assert.Equal("someModel", update.ModelId);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdated, update.Kind);

        Assert.Equal(withUsage ? 3 : 2, update.Contents.Count);
        Assert.Equal("audio/mpeg", Assert.IsType<DataContent>(update.Contents[0]).MediaType);
        Assert.Equal("audio/wav", Assert.IsType<DataContent>(update.Contents[1]).MediaType);

        Assert.NotNull(update.AdditionalProperties);
        Assert.Equal("value1", update.AdditionalProperties["key1"]);
        Assert.Equal(42, update.AdditionalProperties["key2"]);

        if (withUsage)
        {
            var usage = Assert.IsType<UsageContent>(update.Contents[2]);
            Assert.Equal(100, usage.Details.InputTokenCount);
            Assert.Equal(200, usage.Details.OutputTokenCount);
            Assert.Equal(300, usage.Details.TotalTokenCount);
        }
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "responseId": "resp1",
              "modelId": "tts-1",
              "contents": [
                {
                  "$type": "data",
                  "uri": "data:audio/mpeg;base64,AQID"
                }
              ],
              "usage": {
                "inputTokenCount": 100,
                "outputTokenCount": 50
              },
              "additionalProperties": {
                "key": "val"
              }
            }
            """;

        TextToSpeechResponse? result = JsonSerializer.Deserialize<TextToSpeechResponse>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        Assert.Equal("resp1", result.ResponseId);
        Assert.Equal("tts-1", result.ModelId);
        Assert.Single(result.Contents);
        var dataContent = Assert.IsType<DataContent>(result.Contents[0]);
        Assert.Equal("audio/mpeg", dataContent.MediaType);
        Assert.NotNull(result.Usage);
        Assert.Equal(100, result.Usage.InputTokenCount);
        Assert.Equal(50, result.Usage.OutputTokenCount);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal("val", result.AdditionalProperties["key"]?.ToString());
    }
}
