// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGeneratorTests
{
    [Fact]
    public void GetService_WithServiceKey_ReturnsNull()
    {
        using var generator = new TestImageGenerator();
        generator.GetServiceCallback = (serviceType, serviceKey) =>
        {
            // When serviceKey is not null, should return null per interface contract
            return serviceKey is not null ? null : new object();
        };

        var result = generator.GetService(typeof(object), "someKey");
        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithoutServiceKey_CallsCallback()
    {
        using var generator = new TestImageGenerator();
        var expectedResult = new object();

        generator.GetServiceCallback = (serviceType, serviceKey) =>
        {
            Assert.Equal(typeof(object), serviceType);
            Assert.Null(serviceKey);
            return expectedResult;
        };

        var result = generator.GetService(typeof(object));
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task GenerateImagesAsync_CallsCallback()
    {
        var expectedResponse = new ImageGenerationResponse();
        var expectedOptions = new ImageGenerationOptions();
        using var cts = new CancellationTokenSource();
        var expectedRequest = new ImageGenerationRequest("test prompt");

        using var generator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await generator.GenerateImagesAsync(expectedRequest, expectedOptions, cts.Token);
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateImagesAsync_NoCallback_ReturnsEmptyResponse()
    {
        using var generator = new TestImageGenerator();
        var result = await generator.GenerateImagesAsync(new ImageGenerationRequest("test prompt"), null);
        Assert.NotNull(result);
        Assert.Empty(result.Contents);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        var generator = new TestImageGenerator();
        Assert.False(generator.DisposeInvoked);

        generator.Dispose();
        Assert.True(generator.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        var generator = new TestImageGenerator();

        generator.Dispose();
        Assert.True(generator.DisposeInvoked);

        // Second dispose should not throw
#pragma warning disable S3966
        generator.Dispose();
#pragma warning restore S3966
        Assert.True(generator.DisposeInvoked);
    }

    [Fact]
    public async Task GenerateImagesAsync_WithOptions_PassesThroughCorrectly()
    {
        var options = new ImageGenerationOptions
        {
            Background = "transparent",
            ResponseFormat = ImageGenerationResponseFormat.Data,
            Count = 3,
            ImageSize = new Size(1024, 768),
            MediaType = "image/png",
            ModelId = "test-model",
            Style = "photorealistic"
        };

        var expectedRequest = new ImageGenerationRequest("test prompt");

        using var generator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, receivedOptions, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(options, receivedOptions);
                return Task.FromResult(new ImageGenerationResponse());
            }
        };

        await generator.GenerateImagesAsync(expectedRequest, options);
    }

    [Fact]
    public async Task GenerateImagesAsync_WithEditRequest_PassesThroughCorrectly()
    {
        var options = new ImageGenerationOptions
        {
            Background = "opaque",
            ResponseFormat = ImageGenerationResponseFormat.Uri,
            Count = 2,
            MediaType = "image/jpeg",
            ModelId = "edit-model",
            Style = "artistic"
        };

        AIContent[] originalImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];
        var expectedRequest = new ImageGenerationRequest("edit prompt", originalImages);

        using var generator = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, receivedOptions, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(options, receivedOptions);
                return Task.FromResult(new ImageGenerationResponse());
            }
        };

        await generator.GenerateImagesAsync(expectedRequest, options);
    }
}
