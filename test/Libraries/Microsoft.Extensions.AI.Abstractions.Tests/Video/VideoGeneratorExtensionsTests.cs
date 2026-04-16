// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGeneratorExtensionsTests
{
    [Fact]
    public void GetService_Generic_NullGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>("generator", () => ((IVideoGenerator)null!).GetService<IVideoGenerator>());
    }

    [Fact]
    public void GetService_Generic_ReturnsService()
    {
        using var generator = new TestVideoGenerator();
        var result = generator.GetService<IVideoGenerator>();
        Assert.Same(generator, result);
    }

    [Fact]
    public void GetRequiredService_NullGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>("generator", () => ((IVideoGenerator)null!).GetRequiredService(typeof(IVideoGenerator)));
    }

    [Fact]
    public void GetRequiredService_NullType_Throws()
    {
        using var generator = new TestVideoGenerator();
        Assert.Throws<ArgumentNullException>("serviceType", () => generator.GetRequiredService(null!));
    }

    [Fact]
    public void GetRequiredService_ServiceNotAvailable_Throws()
    {
        using var generator = new TestVideoGenerator();
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService(typeof(string)));
    }

    [Fact]
    public void GetRequiredService_Generic_ServiceNotAvailable_Throws()
    {
        using var generator = new TestVideoGenerator();
        Assert.Throws<InvalidOperationException>(() => generator.GetRequiredService<string>());
    }

    [Fact]
    public async Task GenerateVideosAsync_NullGenerator_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("generator", () =>
            ((IVideoGenerator)null!).GenerateVideoAsync("Test"));
    }

    [Fact]
    public async Task GenerateVideosAsync_NullPrompt_Throws()
    {
        using var generator = new TestVideoGenerator();
        await Assert.ThrowsAsync<ArgumentNullException>("prompt", () =>
            generator.GenerateVideoAsync(null!));
    }

    [Fact]
    public async Task GenerateVideosAsync_CallsGenerateAsync()
    {
        VideoGenerationRequest? capturedRequest = null;
        using var generator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) =>
            {
                capturedRequest = request;
                return Task.FromResult<VideoGenerationOperation>(new TestVideoGenerationOperation());
            }
        };

        await generator.GenerateVideoAsync("A cat video");

        Assert.NotNull(capturedRequest);
        Assert.Equal("A cat video", capturedRequest!.Prompt);
        Assert.Null(capturedRequest.StartFrame);
    }

    [Fact]
    public async Task EditVideosAsync_NullGenerator_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("generator", () =>
            ((IVideoGenerator)null!).EditVideoAsync(new DataContent("dGVzdA=="u8.ToArray(), "video/mp4"), "prompt"));
    }

    [Fact]
    public async Task EditVideosAsync_NullSourceVideo_Throws()
    {
        using var generator = new TestVideoGenerator();
        await Assert.ThrowsAsync<ArgumentNullException>("sourceVideo", () =>
            generator.EditVideoAsync((AIContent)null!, "prompt"));
    }

    [Fact]
    public async Task EditVideosAsync_NullPrompt_Throws()
    {
        using var generator = new TestVideoGenerator();
        await Assert.ThrowsAsync<ArgumentNullException>("prompt", () =>
            generator.EditVideoAsync(new DataContent("dGVzdA=="u8.ToArray(), "video/mp4"), null!));
    }

    [Fact]
    public async Task EditVideoAsync_DataContent_CallsGenerateAsync()
    {
        VideoGenerationRequest? capturedRequest = null;
        using var generator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) =>
            {
                capturedRequest = request;
                return Task.FromResult<VideoGenerationOperation>(new TestVideoGenerationOperation());
            }
        };

        var originalVideo = new DataContent("dGVzdA=="u8.ToArray(), "video/mp4");
        await generator.EditVideoAsync(originalVideo, "Make it faster");

        Assert.NotNull(capturedRequest);
        Assert.Equal("Make it faster", capturedRequest!.Prompt);
        Assert.NotNull(capturedRequest.SourceVideo);
    }

    [Fact]
    public async Task EditVideoAsync_ByteArray_CallsGenerateAsync()
    {
        VideoGenerationRequest? capturedRequest = null;
        using var generator = new TestVideoGenerator
        {
            GenerateVideosAsyncCallback = (request, options, ct) =>
            {
                capturedRequest = request;
                return Task.FromResult<VideoGenerationOperation>(new TestVideoGenerationOperation());
            }
        };

        await generator.EditVideoAsync(new byte[] { 1, 2, 3, 4 }, "test.mp4", "Add effects");

        Assert.NotNull(capturedRequest);
        Assert.Equal("Add effects", capturedRequest!.Prompt);
        Assert.NotNull(capturedRequest.SourceVideo);
    }

    [Fact]
    public async Task EditVideoAsync_ByteArray_NullFileName_Throws()
    {
        using var generator = new TestVideoGenerator();
        await Assert.ThrowsAsync<ArgumentNullException>("fileName", () =>
            generator.EditVideoAsync(new byte[] { 1 }, null!, "prompt"));
    }

    [Fact]
    public async Task EditVideoAsync_ByteArray_NullPrompt_Throws()
    {
        using var generator = new TestVideoGenerator();
        await Assert.ThrowsAsync<ArgumentNullException>("prompt", () =>
            generator.EditVideoAsync(new byte[] { 1 }, "test.mp4", null!));
    }
}
