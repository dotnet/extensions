// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGenerationOptionsTests
{
    [Fact]
    public void Constructor_Defaults()
    {
        var options = new VideoGenerationOptions();
        Assert.Null(options.Count);
        Assert.Null(options.Duration);
        Assert.Null(options.FramesPerSecond);
        Assert.Null(options.MediaType);
        Assert.Null(options.ModelId);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.ResponseFormat);
        Assert.Null(options.VideoSize);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        var options = new VideoGenerationOptions
        {
            Count = 3,
            Duration = TimeSpan.FromSeconds(15),
            FramesPerSecond = 30,
            MediaType = "video/webm",
            ModelId = "sora",
            ResponseFormat = VideoGenerationResponseFormat.Data,
            VideoSize = new Size(1280, 720),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        Assert.Equal(3, options.Count);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Duration);
        Assert.Equal(30, options.FramesPerSecond);
        Assert.Equal("video/webm", options.MediaType);
        Assert.Equal("sora", options.ModelId);
        Assert.Equal(VideoGenerationResponseFormat.Data, options.ResponseFormat);
        Assert.Equal(new Size(1280, 720), options.VideoSize);
        Assert.Equal("value", options.AdditionalProperties["key"]);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new VideoGenerationOptions
        {
            Count = 2,
            Duration = TimeSpan.FromSeconds(5),
            FramesPerSecond = 24,
            MediaType = "video/mp4",
            ModelId = "model-1",
            ResponseFormat = VideoGenerationResponseFormat.Uri,
            VideoSize = new Size(1920, 1080),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.Count, clone.Count);
        Assert.Equal(original.Duration, clone.Duration);
        Assert.Equal(original.FramesPerSecond, clone.FramesPerSecond);
        Assert.Equal(original.MediaType, clone.MediaType);
        Assert.Equal(original.ModelId, clone.ModelId);
        Assert.Equal(original.ResponseFormat, clone.ResponseFormat);
        Assert.Equal(original.VideoSize, clone.VideoSize);
        Assert.NotSame(original.AdditionalProperties, clone.AdditionalProperties);
    }

    [Fact]
    public void Clone_FromNull_ReturnsDefaults()
    {
        var options = new DerivedVideoGenerationOptions(null);
        Assert.Null(options.Count);
        Assert.Null(options.Duration);
        Assert.Null(options.ModelId);
    }

    [Theory]
    [InlineData(VideoGenerationResponseFormat.Uri)]
    [InlineData(VideoGenerationResponseFormat.Data)]
    [InlineData(VideoGenerationResponseFormat.Hosted)]
    public void ResponseFormat_EnumValues(VideoGenerationResponseFormat format)
    {
        var options = new VideoGenerationOptions { ResponseFormat = format };
        Assert.Equal(format, options.ResponseFormat);
    }

    [Fact]
    public void JsonSerialization_Roundtrip()
    {
        var options = new VideoGenerationOptions
        {
            Count = 2,
            Duration = TimeSpan.FromSeconds(10),
            FramesPerSecond = 24,
            MediaType = "video/mp4",
            ModelId = "test-model",
            VideoSize = new Size(640, 480),
            ResponseFormat = VideoGenerationResponseFormat.Data,
            AdditionalProperties = new() { ["custom"] = "prop" },
        };

        string json = JsonSerializer.Serialize(options, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<VideoGenerationOptions>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(options.Count, deserialized!.Count);
        Assert.Equal(options.MediaType, deserialized.MediaType);
        Assert.Equal(options.ModelId, deserialized.ModelId);
        Assert.Equal(options.ResponseFormat, deserialized.ResponseFormat);
    }

    private class DerivedVideoGenerationOptions : VideoGenerationOptions
    {
        public DerivedVideoGenerationOptions(VideoGenerationOptions? other)
            : base(other)
        {
        }
    }
}
