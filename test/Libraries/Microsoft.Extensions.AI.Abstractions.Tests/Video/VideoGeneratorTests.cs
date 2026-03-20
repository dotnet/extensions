// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGeneratorTests
{
    [Fact]
    public void GetService_WithServiceKey_ReturnsNull()
    {
        using var generator = new TestVideoGenerator();
        Assert.Null(generator.GetService(typeof(IVideoGenerator), "key"));
    }

    [Fact]
    public void GetService_WithoutServiceKey_CallsCallback()
    {
        using var generator = new TestVideoGenerator();
        var result = generator.GetService(typeof(IVideoGenerator));
        Assert.Same(generator, result);
    }

    [Fact]
    public async Task GenerateVideosAsync_CallsCallback()
    {
        var expectedRequest = new VideoGenerationRequest("Test prompt");
        var expectedResponse = new VideoGenerationResponse();

        using var generator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) =>
            {
                Assert.Same(expectedRequest, request);
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await generator.GenerateAsync(expectedRequest);
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateVideosAsync_NoCallback_ReturnsEmptyResponse()
    {
        using var generator = new TestVideoGenerator();
        var result = await generator.GenerateAsync(new VideoGenerationRequest("Test"));
        Assert.NotNull(result);
        Assert.Empty(result.Contents);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        var generator = new TestVideoGenerator();
        Assert.False(generator.DisposeInvoked);
        generator.Dispose();
        Assert.True(generator.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        var generator = new TestVideoGenerator();
        generator.Dispose();
        generator.Dispose(); // Should not throw
        Assert.True(generator.DisposeInvoked);
    }

    [Fact]
    public async Task GenerateVideosAsync_WithOptions_PassesThroughCorrectly()
    {
        var options = new VideoGenerationOptions
        {
            Count = 2,
            VideoSize = new Size(1920, 1080),
            MediaType = "video/mp4",
            ModelId = "sora",
            Duration = TimeSpan.FromSeconds(10),
            FramesPerSecond = 24,
            ResponseFormat = VideoGenerationResponseFormat.Data
        };

        VideoGenerationOptions? capturedOptions = null;

        using var generator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, opts, ct) =>
            {
                capturedOptions = opts;
                return Task.FromResult(new VideoGenerationResponse());
            }
        };

        await generator.GenerateAsync(new VideoGenerationRequest("Test"), options);

        Assert.NotNull(capturedOptions);
        Assert.Equal(2, capturedOptions!.Count);
        Assert.Equal(new Size(1920, 1080), capturedOptions.VideoSize);
        Assert.Equal("video/mp4", capturedOptions.MediaType);
        Assert.Equal("sora", capturedOptions.ModelId);
        Assert.Equal(TimeSpan.FromSeconds(10), capturedOptions.Duration);
        Assert.Equal(24, capturedOptions.FramesPerSecond);
        Assert.Equal(VideoGenerationResponseFormat.Data, capturedOptions.ResponseFormat);
    }

    [Fact]
    public async Task GenerateVideosAsync_WithEditRequest_PassesThroughCorrectly()
    {
        var originalVideos = new AIContent[] { new DataContent("dGVzdA=="u8.ToArray(), "video/mp4") };
        var request = new VideoGenerationRequest("Edit this", originalVideos);

        VideoGenerationRequest? capturedRequest = null;

        using var generator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (req, opts, ct) =>
            {
                capturedRequest = req;
                return Task.FromResult(new VideoGenerationResponse());
            }
        };

        await generator.GenerateAsync(request);

        Assert.NotNull(capturedRequest);
        Assert.Equal("Edit this", capturedRequest!.Prompt);
        Assert.NotNull(capturedRequest.OriginalMedia);
    }
}
