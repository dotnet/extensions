// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageClientTests
{
    [Fact]
    public void GetService_WithServiceKey_ReturnsNull()
    {
        using var client = new TestImageClient();
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
        using var client = new TestImageClient();
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
        var expectedResponse = new ImageResponse();
        var expectedOptions = new ImageOptions();
        using var cts = new CancellationTokenSource();
        var expectedRequest = new ImageRequest("test prompt");

        using var client = new TestImageClient
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await client.GenerateImagesAsync(expectedRequest, expectedOptions, cts.Token);
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateImagesAsync_NoCallback_ReturnsEmptyResponse()
    {
        using var client = new TestImageClient();
        var result = await client.GenerateImagesAsync(new ImageRequest("test prompt"), null);
        Assert.NotNull(result);
        Assert.Empty(result.Contents);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        var client = new TestImageClient();
        Assert.False(client.DisposeInvoked);

        client.Dispose();
        Assert.True(client.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        var client = new TestImageClient();

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
        var options = new ImageOptions
        {
            Background = "transparent",
            ResponseFormat = ImageResponseFormat.Data,
            Count = 3,
            ImageSize = new Size(1024, 768),
            MediaType = "image/png",
            ModelId = "test-model",
            Style = "photorealistic"
        };

        var expectedRequest = new ImageRequest("test prompt");

        using var client = new TestImageClient
        {
            GenerateImagesAsyncCallback = (request, receivedOptions, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(options, receivedOptions);
                return Task.FromResult(new ImageResponse());
            }
        };

        await client.GenerateImagesAsync(expectedRequest, options);
    }

    [Fact]
    public async Task GenerateImagesAsync_WithEditRequest_PassesThroughCorrectly()
    {
        var options = new ImageOptions
        {
            Background = "opaque",
            ResponseFormat = ImageResponseFormat.Uri,
            Count = 2,
            MediaType = "image/jpeg",
            ModelId = "edit-model",
            Style = "artistic"
        };

        AIContent[] originalImages = [new DataContent((byte[])[1, 2, 3, 4], "image/png")];
        var expectedRequest = new ImageRequest("edit prompt", originalImages);

        using var client = new TestImageClient
        {
            GenerateImagesAsyncCallback = (request, receivedOptions, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(options, receivedOptions);
                return Task.FromResult(new ImageResponse());
            }
        };

        await client.GenerateImagesAsync(expectedRequest, options);
    }
}
