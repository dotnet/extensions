// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToImageOptionsTests
{
    [Fact]
    public void Constructor_Parameterless_PropsDefaulted()
    {
        TextToImageOptions options = new();
        Assert.Equal(TextToImageContentType.Uri, options.ContentType);
        Assert.Equal(1, options.Count);
        Assert.Null(options.GuidanceScale);
        Assert.Equal(default, options.ImageSize);
        Assert.Null(options.ModelId);
        Assert.Null(options.NegativePrompt);
        Assert.Null(options.Steps);
        Assert.Null(options.RawRepresentationFactory);

        TextToImageOptions clone = options.Clone();
        Assert.Equal(TextToImageContentType.Uri, clone.ContentType);
        Assert.Equal(1, clone.Count);
        Assert.Null(clone.GuidanceScale);
        Assert.Equal(default, options.ImageSize);
        Assert.Null(clone.ModelId);
        Assert.Null(clone.NegativePrompt);
        Assert.Null(clone.Steps);
        Assert.Null(clone.RawRepresentationFactory);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        TextToImageOptions options = new();

        Func<ITextToImageClient, object?> factory = client => new { Representation = "raw data" };

        options.ContentType = TextToImageContentType.Data;
        options.Count = 5;
        options.GuidanceScale = 7.5f;
        options.ImageSize = new Size(1024, 768);
        options.ModelId = "modelId";
        options.NegativePrompt = "negative prompt";
        options.Steps = 50;
        options.RawRepresentationFactory = factory;

        Assert.Equal(TextToImageContentType.Data, options.ContentType);
        Assert.Equal(5, options.Count);
        Assert.Equal(7.5f, options.GuidanceScale);
        Assert.Equal(new Size(1024, 768), options.ImageSize);
        Assert.Equal("modelId", options.ModelId);
        Assert.Equal("negative prompt", options.NegativePrompt);
        Assert.Equal(50, options.Steps);
        Assert.Same(factory, options.RawRepresentationFactory);

        TextToImageOptions clone = options.Clone();
        Assert.Equal(TextToImageContentType.Data, clone.ContentType);
        Assert.Equal(5, clone.Count);
        Assert.Equal(7.5f, clone.GuidanceScale);
        Assert.Equal(new Size(1024, 768), clone.ImageSize);
        Assert.Equal("modelId", clone.ModelId);
        Assert.Equal("negative prompt", clone.NegativePrompt);
        Assert.Equal(50, clone.Steps);
        Assert.Same(factory, clone.RawRepresentationFactory);
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        TextToImageOptions options = new()
        {
            ContentType = TextToImageContentType.Data,
            Count = 3,
            GuidanceScale = 10.0f,
            ImageSize = new Size(256, 256),
            ModelId = "test-model",
            NegativePrompt = "bad quality",
            Steps = 25
        };

        string json = JsonSerializer.Serialize(options, TestJsonSerializerContext.Default.TextToImageOptions);

        TextToImageOptions? deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToImageOptions);
        Assert.NotNull(deserialized);

        Assert.Equal(TextToImageContentType.Data, deserialized.ContentType);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal(10.0f, deserialized.GuidanceScale);
        Assert.Equal(new Size(256, 256), deserialized.ImageSize);
        Assert.Equal("test-model", deserialized.ModelId);
        Assert.Equal("bad quality", deserialized.NegativePrompt);
        Assert.Equal(25, deserialized.Steps);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        TextToImageOptions original = new()
        {
            ContentType = TextToImageContentType.Data,
            Count = 2,
            GuidanceScale = 5.0f,
            ImageSize = new Size(512, 512),
            ModelId = "original-model",
            NegativePrompt = "original negative",
            Steps = 30
        };

        TextToImageOptions clone = original.Clone();

        // Modify original
        original.ContentType = TextToImageContentType.Uri;
        original.Count = 1;
        original.GuidanceScale = 2.0f;
        original.ImageSize = new Size(1024, 1024);
        original.ModelId = "modified-model";
        original.NegativePrompt = "modified negative";
        original.Steps = 20;

        // Clone should remain unchanged
        Assert.Equal(TextToImageContentType.Data, clone.ContentType);
        Assert.Equal(2, clone.Count);
        Assert.Equal(5.0f, clone.GuidanceScale);
        Assert.Equal(new Size(512, 512), clone.ImageSize);
        Assert.Equal("original-model", clone.ModelId);
        Assert.Equal("original negative", clone.NegativePrompt);
        Assert.Equal(30, clone.Steps);
    }

    [Theory]
    [InlineData(TextToImageContentType.Uri)]
    [InlineData(TextToImageContentType.Data)]
    public void TextToImageContentType_Values_AreValid(TextToImageContentType contentType)
    {
        Assert.True(Enum.IsDefined(typeof(TextToImageContentType), contentType));
    }

    [Fact]
    public void TextToImageContentType_JsonSerialization_Roundtrips()
    {
        foreach (TextToImageContentType contentType in Enum.GetValues(typeof(TextToImageContentType)))
        {
            string json = JsonSerializer.Serialize(contentType, TestJsonSerializerContext.Default.TextToImageContentType);
            TextToImageContentType deserialized = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.TextToImageContentType);
            Assert.Equal(contentType, deserialized);
        }
    }
}
