// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        ImageOptions options = new();
        Assert.Null(options.Background);
        Assert.Null(options.ResponseFormat);
        Assert.Null(options.Count);
        Assert.Null(options.ImageSize);
        Assert.Null(options.MediaType);
        Assert.Null(options.ModelId);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.Style);

        ImageOptions clone = options.Clone();
        Assert.Null(clone.Background);
        Assert.Null(clone.ResponseFormat);
        Assert.Null(clone.Count);
        Assert.Null(clone.ImageSize);
        Assert.Null(clone.MediaType);
        Assert.Null(clone.ModelId);
        Assert.Null(clone.RawRepresentationFactory);
        Assert.Null(clone.Style);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ImageOptions options = new();

        Func<IImageClient, object?> factory = client => new { Representation = "raw data" };

        options.Background = "transparent";
        options.ResponseFormat = ImageResponseFormat.Data;
        options.Count = 5;
        options.ImageSize = new Size(1024, 768);
        options.MediaType = "image/png";
        options.ModelId = "modelId";
        options.RawRepresentationFactory = factory;
        options.Style = "photorealistic";

        Assert.Equal("transparent", options.Background);
        Assert.Equal(ImageResponseFormat.Data, options.ResponseFormat);
        Assert.Equal(5, options.Count);
        Assert.Equal(new Size(1024, 768), options.ImageSize);
        Assert.Equal("image/png", options.MediaType);
        Assert.Equal("modelId", options.ModelId);
        Assert.Same(factory, options.RawRepresentationFactory);
        Assert.Equal("photorealistic", options.Style);

        ImageOptions clone = options.Clone();
        Assert.Equal("transparent", clone.Background);
        Assert.Equal(ImageResponseFormat.Data, clone.ResponseFormat);
        Assert.Equal(5, clone.Count);
        Assert.Equal(new Size(1024, 768), clone.ImageSize);
        Assert.Equal("image/png", clone.MediaType);
        Assert.Equal("modelId", clone.ModelId);
        Assert.Same(factory, clone.RawRepresentationFactory);
        Assert.Equal("photorealistic", clone.Style);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ImageOptions options = new()
        {
            Background = "opaque",
            ResponseFormat = ImageResponseFormat.Data,
            Count = 3,
            ImageSize = new Size(256, 256),
            MediaType = "image/jpeg",
            ModelId = "test-model",
            Style = "artistic"
        };

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.ImageOptions);

        ImageOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ImageOptions);
        Assert.NotNull(deserialized);

        Assert.Equal("opaque", deserialized.Background);
        Assert.Equal(ImageResponseFormat.Data, deserialized.ResponseFormat);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal(new Size(256, 256), deserialized.ImageSize);
        Assert.Equal("image/jpeg", deserialized.MediaType);
        Assert.Equal("test-model", deserialized.ModelId);
        Assert.Equal("artistic", deserialized.Style);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        ImageOptions original = new()
        {
            Background = "transparent",
            ResponseFormat = ImageResponseFormat.Data,
            Count = 2,
            ImageSize = new Size(512, 512),
            MediaType = "image/png",
            ModelId = "original-model",
            Style = "minimalist"
        };

        ImageOptions clone = original.Clone();

        // Modify original
        original.Background = "opaque";
        original.ResponseFormat = ImageResponseFormat.Uri;
        original.Count = 1;
        original.ImageSize = new Size(1024, 1024);
        original.MediaType = "image/jpeg";
        original.ModelId = "modified-model";
        original.Style = "baroque";

        // Clone should remain unchanged
        Assert.Equal("transparent", clone.Background);
        Assert.Equal(ImageResponseFormat.Data, clone.ResponseFormat);
        Assert.Equal(2, clone.Count);
        Assert.Equal(new Size(512, 512), clone.ImageSize);
        Assert.Equal("image/png", clone.MediaType);
        Assert.Equal("original-model", clone.ModelId);
        Assert.Equal("minimalist", clone.Style);
    }

    [Theory]
    [InlineData(ImageResponseFormat.Uri)]
    [InlineData(ImageResponseFormat.Data)]
    [InlineData(ImageResponseFormat.Hosted)]
    public void ImageResponseFormat_Values_AreValid(ImageResponseFormat responseFormat)
    {
        Assert.True(Enum.IsDefined(typeof(ImageResponseFormat), responseFormat));
    }

    [Fact]
    public void ImageResponseFormat_JsonSerialization_Roundtrips()
    {
        foreach (ImageResponseFormat responseFormat in Enum.GetValues(typeof(ImageResponseFormat)))
        {
            string json = JsonSerializer.Serialize(responseFormat, TestJsonSerializerContext.Default.ImageResponseFormat);
            ImageResponseFormat deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ImageResponseFormat);
            Assert.Equal(responseFormat, deserialized);
        }
    }
}
