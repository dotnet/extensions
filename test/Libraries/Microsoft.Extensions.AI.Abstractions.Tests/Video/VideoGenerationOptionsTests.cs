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
        Assert.Null(options.AspectRatio);
        Assert.Null(options.Count);
        Assert.Null(options.Duration);
        Assert.Null(options.FramesPerSecond);
        Assert.Null(options.GenerateAudio);
        Assert.Null(options.MediaType);
        Assert.Null(options.ModelId);
        Assert.Null(options.RawRepresentationFactory);
        Assert.Null(options.ResponseFormat);
        Assert.Null(options.Seed);
        Assert.Null(options.VideoSize);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        var options = new VideoGenerationOptions
        {
            AspectRatio = "16:9",
            Count = 3,
            Duration = TimeSpan.FromSeconds(15),
            FramesPerSecond = 30,
            GenerateAudio = true,
            MediaType = "video/webm",
            ModelId = "sora",
            ResponseFormat = VideoGenerationResponseFormat.Data,
            Seed = 42,
            VideoSize = new Size(1280, 720),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        Assert.Equal("16:9", options.AspectRatio);
        Assert.Equal(3, options.Count);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Duration);
        Assert.Equal(30, options.FramesPerSecond);
        Assert.True(options.GenerateAudio);
        Assert.Equal("video/webm", options.MediaType);
        Assert.Equal("sora", options.ModelId);
        Assert.Equal(VideoGenerationResponseFormat.Data, options.ResponseFormat);
        Assert.Equal(42, options.Seed);
        Assert.Equal(new Size(1280, 720), options.VideoSize);
        Assert.Equal("value", options.AdditionalProperties["key"]);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new VideoGenerationOptions
        {
            AspectRatio = "9:16",
            Count = 2,
            Duration = TimeSpan.FromSeconds(5),
            FramesPerSecond = 24,
            GenerateAudio = true,
            MediaType = "video/mp4",
            ModelId = "model-1",
            ResponseFormat = VideoGenerationResponseFormat.Uri,
            Seed = 123,
            VideoSize = new Size(1920, 1080),
            AdditionalProperties = new() { ["key"] = "value" },
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.AspectRatio, clone.AspectRatio);
        Assert.Equal(original.Count, clone.Count);
        Assert.Equal(original.Duration, clone.Duration);
        Assert.Equal(original.FramesPerSecond, clone.FramesPerSecond);
        Assert.Equal(original.GenerateAudio, clone.GenerateAudio);
        Assert.Equal(original.MediaType, clone.MediaType);
        Assert.Equal(original.ModelId, clone.ModelId);
        Assert.Equal(original.ResponseFormat, clone.ResponseFormat);
        Assert.Equal(original.Seed, clone.Seed);
        Assert.Equal(original.VideoSize, clone.VideoSize);
        Assert.NotSame(original.AdditionalProperties, clone.AdditionalProperties);
    }

    [Fact]
    public void Clone_FromNull_ReturnsDefaults()
    {
        var options = new DerivedVideoGenerationOptions(null);
        Assert.Null(options.AspectRatio);
        Assert.Null(options.Count);
        Assert.Null(options.Duration);
        Assert.Null(options.GenerateAudio);
        Assert.Null(options.ModelId);
        Assert.Null(options.Seed);
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
            AspectRatio = "1:1",
            Count = 2,
            Duration = TimeSpan.FromSeconds(10),
            FramesPerSecond = 24,
            GenerateAudio = true,
            MediaType = "video/mp4",
            ModelId = "test-model",
            Seed = 99,
            VideoSize = new Size(640, 480),
            ResponseFormat = VideoGenerationResponseFormat.Data,
            AdditionalProperties = new() { ["custom"] = "prop" },
        };

        string json = JsonSerializer.Serialize(options, AIJsonUtilities.DefaultOptions);
        var deserialized = JsonSerializer.Deserialize<VideoGenerationOptions>(json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(options.AspectRatio, deserialized!.AspectRatio);
        Assert.Equal(options.Count, deserialized.Count);
        Assert.Equal(options.GenerateAudio, deserialized.GenerateAudio);
        Assert.Equal(options.MediaType, deserialized.MediaType);
        Assert.Equal(options.ModelId, deserialized.ModelId);
        Assert.Equal(options.ResponseFormat, deserialized.ResponseFormat);
        Assert.Equal(options.Seed, deserialized.Seed);
    }

    private class DerivedVideoGenerationOptions : VideoGenerationOptions
    {
        public DerivedVideoGenerationOptions(VideoGenerationOptions? other)
            : base(other)
        {
        }
    }
}
