// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToImageClientTests
{
    [Fact]
    public void GetService_WithServiceKey_ReturnsNull()
    {
        using var client = new TestTextToImageClient();
        client.GetServiceCallback = (serviceType, serviceKey) =>
        {
            // When serviceKey is not null, should return null per interface contract
            return serviceKey is not null ? null : new object();
        };

        var result = client.GetService(typeof(object), "someKey");
        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithoutServiceKey_CallsCallback()
    {
        using var client = new TestTextToImageClient();
        var expectedResult = new object();

        client.GetServiceCallback = (serviceType, serviceKey) =>
        {
            Assert.Equal(typeof(object), serviceType);
            Assert.Null(serviceKey);
            return expectedResult;
        };

        var result = client.GetService(typeof(object));
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task GenerateImagesAsync_CallsCallback()
    {
        var expectedResponse = new TextToImageResponse();
        var expectedOptions = new TextToImageOptions();
        using var cts = new CancellationTokenSource();

        using var client = new TestTextToImageClient
        {
            GenerateImagesAsyncCallback = (prompt, options, cancellationToken) =>
            {
                Assert.Equal("test prompt", prompt);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await client.GenerateImagesAsync("test prompt", expectedOptions, cts.Token);
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateImagesAsync_NoCallback_ReturnsEmptyResponse()
    {
        using var client = new TestTextToImageClient();
        var result = await client.GenerateImagesAsync("test prompt", null);
        Assert.NotNull(result);
        Assert.Empty(result.Contents);
    }

    [Fact]
    public async Task EditImagesAsync_CallsCallback()
    {
        var expectedResponse = new TextToImageResponse();
        var expectedOptions = new TextToImageOptions();
        using var cts = new CancellationTokenSource();
        AIContent[] expectedImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];

        using var client = new TestTextToImageClient
        {
            EditImagesAsyncCallback = (originalImages, prompt, options, cancellationToken) =>
            {
                Assert.Same(expectedImages, originalImages);
                Assert.Equal("edit prompt", prompt);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await client.EditImagesAsync(expectedImages, "edit prompt", expectedOptions, cts.Token);
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task EditImagesAsync_NoCallback_ReturnsEmptyResponse()
    {
        using var client = new TestTextToImageClient();
        AIContent[] originalImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];

        var result = await client.EditImagesAsync(originalImages, "edit prompt", null);
        Assert.NotNull(result);
        Assert.Empty(result.Contents);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        var client = new TestTextToImageClient();
        Assert.False(client.DisposeInvoked);

        client.Dispose();
        Assert.True(client.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        var client = new TestTextToImageClient();

        client.Dispose();
        Assert.True(client.DisposeInvoked);

        // Second dispose should not throw
#pragma warning disable S3966
        client.Dispose();
#pragma warning restore S3966
        Assert.True(client.DisposeInvoked);
    }

    [Fact]
    public async Task GenerateImagesAsync_WithOptions_PassesThroughCorrectly()
    {
        var options = new TextToImageOptions
        {
            ContentType = TextToImageContentType.Data,
            Count = 3,
            GuidanceScale = 7.5f,
            ImageSize = new Size(1024, 768),
            ModelId = "test-model",
            NegativePrompt = "low quality",
            Steps = 50
        };

        using var client = new TestTextToImageClient
        {
            GenerateImagesAsyncCallback = (prompt, receivedOptions, cancellationToken) =>
            {
                Assert.Same(options, receivedOptions);
                return Task.FromResult(new TextToImageResponse());
            }
        };

        await client.GenerateImagesAsync("test prompt", options);
    }

    [Fact]
    public async Task EditImagesAsync_WithOptions_PassesThroughCorrectly()
    {
        var options = new TextToImageOptions
        {
            ContentType = TextToImageContentType.Uri,
            Count = 2,
            ModelId = "edit-model"
        };

        AIContent[] originalImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];

        using var client = new TestTextToImageClient
        {
            EditImagesAsyncCallback = (dataContent, prompt, receivedOptions, cancellationToken) =>
            {
                Assert.Same(options, receivedOptions);
                return Task.FromResult(new TextToImageResponse());
            }
        };

        await client.EditImagesAsync(originalImages, "edit prompt", options);
    }
}
