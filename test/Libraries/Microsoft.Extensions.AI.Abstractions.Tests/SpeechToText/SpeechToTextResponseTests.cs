// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class SpeechToTextResponseTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("contents", () => new SpeechToTextResponse((IList<AIContent>)null!));
    }

    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        SpeechToTextResponse response = new();
        Assert.Empty(response.Contents);
        Assert.Empty(response.Text);
        Assert.NotNull(response.Contents);
        Assert.Same(response.Contents, response.Contents);
        Assert.Empty(response.Contents);
        Assert.Null(response.RawRepresentation);
        Assert.Null(response.AdditionalProperties);
        Assert.Null(response.StartTime);
        Assert.Null(response.EndTime);
        Assert.Equal(string.Empty, response.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text")]
    public void Constructor_String_PropsRoundtrip(string? text)
    {
        SpeechToTextResponse response = new(text);

        Assert.Same(response.Contents, response.Contents);
        if (text is null)
        {
            Assert.Empty(response.Contents);
        }
        else
        {
            Assert.Single(response.Contents);
            TextContent tc = Assert.IsType<TextContent>(response.Contents[0]);
            Assert.Equal(text, tc.Text);
        }

        Assert.Null(response.RawRepresentation);
        Assert.Null(response.AdditionalProperties);
        Assert.Equal(text ?? string.Empty, response.ToString());
    }

    [Fact]
    public void Constructor_List_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("contents", () => new SpeechToTextResponse((IList<AIContent>)null!));
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
            content.Add(new TextContent($"text-{i}"));
        }

        SpeechToTextResponse response = new(content);

        Assert.Same(response.Contents, response.Contents);
        if (contentCount == 0)
        {
            Assert.Empty(response.Contents);
            Assert.Empty(response.Text);
        }
        else
        {
            Assert.Equal(contentCount, response.Contents.Count);
            for (int i = 0; i < contentCount; i++)
            {
                TextContent tc = Assert.IsType<TextContent>(response.Contents[i]);
                Assert.Equal($"text-{i}", tc.Text);
            }

            Assert.Equal(string.Concat(Enumerable.Range(0, contentCount).Select(i => $"text-{i}")), response.Text);
            Assert.Equal(string.Concat(Enumerable.Range(0, contentCount).Select(i => $"text-{i}")), response.ToString());
        }
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        SpeechToTextResponse response = new();
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

        Assert.Null(response.StartTime);
        TimeSpan startTime = TimeSpan.FromSeconds(1);
        response.StartTime = startTime;
        Assert.Equal(startTime, response.StartTime);

        Assert.Null(response.EndTime);
        TimeSpan endTime = TimeSpan.FromSeconds(2);
        response.EndTime = endTime;
        Assert.Equal(endTime, response.EndTime);

        List<AIContent> newContents = [new TextContent("text1"), new TextContent("text2")];
        response.Contents = newContents;
        Assert.Same(newContents, response.Contents);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        SpeechToTextResponse original = new()
        {
            Contents =
            [
                new TextContent("Text1"),
                new TextContent("Text2"),
                new TextContent("Text3"),
                new TextContent("Text4"),
            ],
            ResponseId = "id",
            ModelId = "modelId",
            StartTime = TimeSpan.FromSeconds(1),
            EndTime = TimeSpan.FromSeconds(2),
            RawRepresentation = new(),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(original, TestJsonSerializerContext.Default.SpeechToTextResponse);

        SpeechToTextResponse? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.SpeechToTextResponse);

        Assert.NotNull(result);
        Assert.Equal(4, result.Contents.Count);

        for (int i = 0; i < original.Contents.Count; i++)
        {
            Assert.Equal($"Text{i + 1}", ((TextContent)result.Contents[i]).Text);
        }

        Assert.Equal("id", result.ResponseId);
        Assert.Equal("modelId", result.ModelId);
        Assert.Equal(TimeSpan.FromSeconds(1), result.StartTime);
        Assert.Equal(TimeSpan.FromSeconds(2), result.EndTime);

        Assert.NotNull(result.AdditionalProperties);
        Assert.Single(result.AdditionalProperties);
        Assert.True(result.AdditionalProperties.TryGetValue("key", out object? value));
        Assert.IsType<JsonElement>(value);
        Assert.Equal("value", ((JsonElement)value!).GetString());
    }

    [Fact]
    public void ToString_OutputsText()
    {
        SpeechToTextResponse response = new("This is a test." + Environment.NewLine + "It's multiple lines.");
        Assert.Equal("This is a test." + Environment.NewLine + "It's multiple lines.", response.ToString());
    }

    [Fact]
    public void ToSpeechToTextResponseUpdates_ReturnsExpectedUpdate()
    {
        // Arrange: create a response with contents
        SpeechToTextResponse response = new()
        {
            Contents =
            [
                new TextContent("Hello, "),
                new DataContent("data:image/png;base64,AQIDBA==", mediaType: "image/png"),
                new TextContent("world!")
            ],
            StartTime = TimeSpan.FromSeconds(1),
            EndTime = TimeSpan.FromSeconds(2),
            ResponseId = "12345",
            ModelId = "someModel",
            AdditionalProperties = new() { ["key1"] = "value1", ["key2"] = 42 },
        };

        // Act: convert to streaming updates
        SpeechToTextResponseUpdate[] updates = response.ToSpeechToTextResponseUpdates();

        // Assert: should be a single update with all properties
        Assert.Single(updates);

        SpeechToTextResponseUpdate update = updates[0];
        Assert.Equal("12345", update.ResponseId);
        Assert.Equal("someModel", update.ModelId);
        Assert.Equal(SpeechToTextResponseUpdateKind.TextUpdated, update.Kind);
        Assert.Equal(TimeSpan.FromSeconds(1), update.StartTime);
        Assert.Equal(TimeSpan.FromSeconds(2), update.EndTime);

        Assert.Equal(3, update.Contents.Count);
        Assert.Equal("Hello, ", Assert.IsType<TextContent>(update.Contents[0]).Text);
        Assert.Equal("image/png", Assert.IsType<DataContent>(update.Contents[1]).MediaType);
        Assert.Equal("world!", Assert.IsType<TextContent>(update.Contents[2]).Text);

        Assert.NotNull(update.AdditionalProperties);
        Assert.Equal("value1", update.AdditionalProperties["key1"]);
        Assert.Equal(42, update.AdditionalProperties["key2"]);
    }
}
