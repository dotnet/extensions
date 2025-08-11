// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGenerationOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        ImageGenerationOptions options = new();
        Assert.Null(options.ResponseFormat);
        Assert.Null(options.Count);
        Assert.Null(options.ImageSize);
        Assert.Null(options.MediaType);
        Assert.Null(options.ModelId);
        Assert.Null(options.RawRepresentationFactory);

        ImageGenerationOptions clone = options.Clone();
        Assert.Null(clone.ResponseFormat);
        Assert.Null(clone.Count);
        Assert.Null(clone.ImageSize);
        Assert.Null(clone.MediaType);
        Assert.Null(clone.ModelId);
        Assert.Null(clone.RawRepresentationFactory);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        ImageGenerationOptions options = new();

        Func<IImageGenerator, object?> factory = generator => new { Representation = "raw data" };

        options.ResponseFormat = ImageGenerationResponseFormat.Data;
        options.Count = 5;
        options.ImageSize = new Size(1024, 768);
        options.MediaType = "image/png";
        options.ModelId = "modelId";
        options.RawRepresentationFactory = factory;

        Assert.Equal(ImageGenerationResponseFormat.Data, options.ResponseFormat);
        Assert.Equal(5, options.Count);
        Assert.Equal(new Size(1024, 768), options.ImageSize);
        Assert.Equal("image/png", options.MediaType);
        Assert.Equal("modelId", options.ModelId);
        Assert.Same(factory, options.RawRepresentationFactory);

        ImageGenerationOptions clone = options.Clone();
        Assert.Equal(ImageGenerationResponseFormat.Data, clone.ResponseFormat);
        Assert.Equal(5, clone.Count);
        Assert.Equal(new Size(1024, 768), clone.ImageSize);
        Assert.Equal("image/png", clone.MediaType);
        Assert.Equal("modelId", clone.ModelId);
        Assert.Same(factory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ImageGenerationOptions options = new()
        {
            ResponseFormat = ImageGenerationResponseFormat.Data,
            Count = 3,
            ImageSize = new Size(256, 256),
            MediaType = "image/jpeg",
            ModelId = "test-model",
        };

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.ImageGenerationOptions);

        ImageGenerationOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ImageGenerationOptions);
        Assert.NotNull(deserialized);

        Assert.Equal(ImageGenerationResponseFormat.Data, deserialized.ResponseFormat);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal(new Size(256, 256), deserialized.ImageSize);
        Assert.Equal("image/jpeg", deserialized.MediaType);
        Assert.Equal("test-model", deserialized.ModelId);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        ImageGenerationOptions original = new()
        {
            ResponseFormat = ImageGenerationResponseFormat.Data,
            Count = 2,
            ImageSize = new Size(512, 512),
            MediaType = "image/png",
            ModelId = "original-model"
        };

        ImageGenerationOptions clone = original.Clone();

        // Modify original
        original.ResponseFormat = ImageGenerationResponseFormat.Uri;
        original.Count = 1;
        original.ImageSize = new Size(1024, 1024);
        original.MediaType = "image/jpeg";
        original.ModelId = "modified-model";

        // Clone should remain unchanged
        Assert.Equal(ImageGenerationResponseFormat.Data, clone.ResponseFormat);
        Assert.Equal(2, clone.Count);
        Assert.Equal(new Size(512, 512), clone.ImageSize);
        Assert.Equal("image/png", clone.MediaType);
        Assert.Equal("original-model", clone.ModelId);
    }

    [Theory]
    [InlineData(ImageGenerationResponseFormat.Uri)]
    [InlineData(ImageGenerationResponseFormat.Data)]
    [InlineData(ImageGenerationResponseFormat.Hosted)]
    public void ImageGenerationResponseFormat_Values_AreValid(ImageGenerationResponseFormat responseFormat)
    {
        Assert.True(Enum.IsDefined(typeof(ImageGenerationResponseFormat), responseFormat));
    }

    [Fact]
    public void ImageGenerationResponseFormat_JsonSerialization_Roundtrips()
    {
        foreach (ImageGenerationResponseFormat responseFormat in Enum.GetValues(typeof(ImageGenerationResponseFormat)))
        {
            string json = JsonSerializer.Serialize(responseFormat, TestJsonSerializerContext.Default.ImageGenerationResponseFormat);
            ImageGenerationResponseFormat deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ImageGenerationResponseFormat);
            Assert.Equal(responseFormat, deserialized);
        }
    }
}
