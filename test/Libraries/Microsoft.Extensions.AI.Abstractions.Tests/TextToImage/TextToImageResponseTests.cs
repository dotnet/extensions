// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToImageResponseTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        TextToImageResponse response = new();
        Assert.Empty(response.Contents);
        Assert.NotNull(response.Contents);
        Assert.Same(response.Contents, response.Contents);
        Assert.Empty(response.Contents);
        Assert.Null(response.RawRepresentation);
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
            content.Add(new UriContent(new Uri($"https://example.com/image-{i}.png"), "image/png"));
        }

        TextToImageResponse response = new(content);

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
                UriContent uc = Assert.IsType<UriContent>(response.Contents[i]);
                Assert.Equal($"https://example.com/image-{i}.png", uc.Uri.ToString());
                Assert.Equal("image/png", uc.MediaType);
            }
        }
    }

    [Fact]
    public void Contents_SetNull_ReturnsEmpty()
    {
        TextToImageResponse response = new()
        {
            Contents = null!
        };
        Assert.NotNull(response.Contents);
        Assert.Empty(response.Contents);
    }

    [Fact]
    public void Contents_Set_Roundtrips()
    {
        TextToImageResponse response = new();
        byte[] imageData = [1, 2, 3, 4];

        List<AIContent> contents = [
            new UriContent(new Uri("https://example.com/image1.png"), "image/png"),
            new DataContent(imageData, "image/jpeg")
        ];

        response.Contents = contents;
        Assert.Same(contents, response.Contents);
    }

    [Fact]
    public void RawRepresentation_Roundtrips()
    {
        TextToImageResponse response = new();
        Assert.Null(response.RawRepresentation);

        object representation = new { test = "value" };
        response.RawRepresentation = representation;
        Assert.Same(representation, response.RawRepresentation);

        response.RawRepresentation = null;
        Assert.Null(response.RawRepresentation);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        List<AIContent> contents = [
            new UriContent(new Uri("https://example.com/image1.png"), "image/png"),
            new DataContent((byte[])[1, 2, 3, 4], "image/jpeg")
        ];

        TextToImageResponse response = new(contents);

        string json = JsonSerializer.Serialize(response, TestJsonSerializerContext.Default.TextToImageResponse);

        TextToImageResponse? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToImageResponse);
        Assert.NotNull(deserialized);

        Assert.Equal(2, deserialized.Contents.Count);

        UriContent uriContent = Assert.IsType<UriContent>(deserialized.Contents[0]);
        Assert.Equal("https://example.com/image1.png", uriContent.Uri.ToString());
        Assert.Equal("image/png", uriContent.MediaType);

        DataContent dataContent = Assert.IsType<DataContent>(deserialized.Contents[1]);
        Assert.Equal([1, 2, 3, 4], dataContent.Data.ToArray());
        Assert.Equal("image/jpeg", dataContent.MediaType);
    }

    [Fact]
    public void JsonSerialization_Empty_Roundtrips()
    {
        TextToImageResponse response = new();

        string json = JsonSerializer.Serialize(response, TestJsonSerializerContext.Default.TextToImageResponse);

        TextToImageResponse? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToImageResponse);
        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Contents);
    }

    [Fact]
    public void JsonSerialization_WithVariousContentTypes_Roundtrips()
    {
        List<AIContent> contents = [
            new UriContent(new Uri("https://example.com/image.png"), "image/png"),
            new DataContent((byte[])[255, 216, 255, 224], "image/jpeg"),
            new TextContent("Generated image description") // Edge case: text content in image response
        ];

        TextToImageResponse response = new(contents);

        string json = JsonSerializer.Serialize(response, TestJsonSerializerContext.Default.TextToImageResponse);

        TextToImageResponse? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToImageResponse);
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Contents.Count);

        Assert.IsType<UriContent>(deserialized.Contents[0]);
        Assert.IsType<DataContent>(deserialized.Contents[1]);
        Assert.IsType<TextContent>(deserialized.Contents[2]);
    }
}
