// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToImageClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = TextToImageClientExtensions.GetService<object>(null!);
        });
    }

    [Fact]
    public void GetService_ValidClient_CallsUnderlyingGetService()
    {
        using var testClient = new TestTextToImageClient();
        var expectedResult = new object();
        var expectedServiceKey = new object();

        testClient.GetServiceCallback = (serviceType, serviceKey) =>
        {
            Assert.Equal(typeof(object), serviceType);
            Assert.Same(expectedServiceKey, serviceKey);
            return expectedResult;
        };

        var result = testClient.GetService<object>(expectedServiceKey);
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public void GetService_ReturnsCorrectType()
    {
        using var testClient = new TestTextToImageClient();
        var metadata = new TextToImageClientMetadata("test", null, "model");

        testClient.GetServiceCallback = (serviceType, serviceKey) =>
        {
            return (serviceType == typeof(TextToImageClientMetadata)) ? metadata : null;
        };

        var result = testClient.GetService<TextToImageClientMetadata>();
        Assert.Same(metadata, result);

        var nullResult = testClient.GetService<string>();
        Assert.Null(nullResult);
    }

    [Fact]
    public async Task GenerateEditImageAsync_DataContent_InvalidArgs_Throws()
    {
        ITextToImageClient? client = null;
        var imageData = new DataContent((byte[])[1, 2, 3, 4], "image/png");

        var ex1 = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TextToImageClientExtensions.GenerateEditImageAsync(client!, imageData, "image.png", "prompt"));
        Assert.Equal("client", ex1.ParamName);

        using var testClient = new TestTextToImageClient();
        DataContent? nullContent = null;
        var ex2 = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TextToImageClientExtensions.GenerateEditImageAsync(testClient, nullContent!, "image.png", "prompt"));
        Assert.Equal("originalImage", ex2.ParamName);
    }

    [Fact]
    public async Task GenerateEditImageAsync_DataContent_CallsUnderlyingMethod()
    {
        var imageData = new byte[] { 255, 216, 255, 224, 0, 16, 74, 70, 73, 70 }; // JPEG header
        var dataContent = new DataContent(imageData, "image/jpeg");
        var expectedResponse = new TextToImageResponse();
        var expectedOptions = new TextToImageOptions();
        using var cts = new CancellationTokenSource();

        using var testClient = new TestTextToImageClient
        {
            GenerateEditImageAsyncCallback = (originalImage, originalImageFileName, prompt, options, cancellationToken) =>
            {
                Assert.NotNull(originalImage);
                Assert.Equal("test.jpg", originalImageFileName);
                Assert.Equal("edit this image", prompt);
                Assert.Same(expectedOptions, options);
                Assert.Equal(cts.Token, cancellationToken);

                // Verify stream contains expected data
                using var memoryStream = new MemoryStream();
                originalImage.CopyTo(memoryStream);
                Assert.Equal(imageData, memoryStream.ToArray());

                return Task.FromResult(expectedResponse);
            }
        };

        var result = await testClient.GenerateEditImageAsync(
            dataContent,
            "test.jpg",
            "edit this image",
            expectedOptions,
            cts.Token);

        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateEditImageAsync_DataContent_WithDefaults_Works()
    {
        var imageData = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }; // PNG header
        var dataContent = new DataContent(imageData, "image/png");
        var expectedResponse = new TextToImageResponse();

        using var testClient = new TestTextToImageClient
        {
            GenerateEditImageAsyncCallback = (originalImage, originalImageFileName, prompt, options, cancellationToken) =>
            {
                Assert.NotNull(originalImage);
                Assert.Equal("image.png", originalImageFileName);
                Assert.Equal("transform image", prompt);
                Assert.Null(options);
                Assert.Equal(CancellationToken.None, cancellationToken);
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await testClient.GenerateEditImageAsync(dataContent, "image.png", "transform image");
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateEditImageAsync_DataContent_EmptyData_Works()
    {
        var dataContent = new DataContent((byte[])[], "image/png");
        var expectedResponse = new TextToImageResponse();

        using var testClient = new TestTextToImageClient
        {
            GenerateEditImageAsyncCallback = (originalImage, originalImageFileName, prompt, options, cancellationToken) =>
            {
                using var memoryStream = new MemoryStream();
                originalImage.CopyTo(memoryStream);
                Assert.Empty(memoryStream.ToArray());
                return Task.FromResult(expectedResponse);
            }
        };

        var result = await testClient.GenerateEditImageAsync(dataContent, "empty.png", "prompt");
        Assert.Same(expectedResponse, result);
    }
}
