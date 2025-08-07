// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = ImageClientExtensions.GetService<object>(null!);
        });
    }

    [Fact]
    public void GetService_ValidClient_CallsUnderlyingGetService()
    {
        using var testClient = new TestImageClient();
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
        using var testClient = new TestImageClient();
        var metadata = new ImageClientMetadata("test", null, "model");

        testClient.GetServiceCallback = (serviceType, serviceKey) =>
        {
            return (serviceType == typeof(ImageClientMetadata)) ? metadata : null;
        };

        var result = testClient.GetService<ImageClientMetadata>();
        Assert.Same(metadata, result);

        var nullResult = testClient.GetService<string>();
        Assert.Null(nullResult);
    }

    [Fact]
    public async Task EditImageAsync_DataContent_CallsGenerateImagesAsync()
    {
        // Arrange
        using var testClient = new TestImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var dataContent = new DataContent(imageData, "image/png") { Name = "test.png" };
        var prompt = "Edit this image";
        var options = new ImageOptions { Count = 2 };
        var expectedResponse = new ImageResponse();
        var cancellationToken = new CancellationToken(canceled: false);

        testClient.GenerateImagesAsyncCallback = (request, o, ct) =>
        {
            Assert.NotNull(request.OriginalImages);
            Assert.Single(request.OriginalImages);
            Assert.Same(dataContent, Assert.Single(request.OriginalImages));
            Assert.Equal(prompt, request.Prompt);
            Assert.Same(options, o);
            Assert.Equal(cancellationToken, ct);
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await testClient.EditImageAsync(dataContent, prompt, options, cancellationToken);

        // Assert
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task EditImageAsync_DataContent_NullArguments_Throws()
    {
        using var testClient = new TestImageClient();
        var dataContent = new DataContent(new byte[] { 1, 2, 3 }, "image/png");

        await Assert.ThrowsAsync<ArgumentNullException>("client", async () =>
            await ImageClientExtensions.EditImageAsync(null!, dataContent, "prompt"));

        await Assert.ThrowsAsync<ArgumentNullException>("originalImage", async () =>
            await testClient.EditImageAsync(null!, "prompt"));

        await Assert.ThrowsAsync<ArgumentNullException>("prompt", async () =>
            await testClient.EditImageAsync(dataContent, null!));
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_CallsGenerateImagesAsync()
    {
        // Arrange
        using var testClient = new TestImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var fileName = "test.jpg";
        var prompt = "Edit this image";
        var options = new ImageOptions { Count = 2 };
        var expectedResponse = new ImageResponse();
        var cancellationToken = new CancellationToken(canceled: false);

        testClient.GenerateImagesAsyncCallback = (request, o, ct) =>
        {
            Assert.NotNull(request.OriginalImages);
            Assert.Single(request.OriginalImages);
            var dataContent = Assert.IsType<DataContent>(Assert.Single(request.OriginalImages));
            Assert.Equal(imageData, dataContent.Data.ToArray());
            Assert.Equal("image/jpeg", dataContent.MediaType);
            Assert.Equal(fileName, dataContent.Name);
            Assert.Equal(prompt, request.Prompt);
            Assert.Same(options, o);
            Assert.Equal(cancellationToken, ct);
            return Task.FromResult(expectedResponse);
        };

        // Act
        var result = await testClient.EditImageAsync(imageData, fileName, prompt, options, cancellationToken);

        // Assert
        Assert.Same(expectedResponse, result);
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullClient_Throws()
    {
        var imageData = new byte[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentNullException>("client", async () =>
            await ImageClientExtensions.EditImageAsync(null!, imageData, "test.png", "prompt"));
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullData_Throws()
    {
        using var testClient = new TestImageClient();

        await Assert.ThrowsAsync<ArgumentNullException>("originalImageData", async () =>
        {
            byte[] nullData = null!;
            await testClient.EditImageAsync(nullData, "test.png", "prompt");
        });
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullFileName_Throws()
    {
        using var testClient = new TestImageClient();
        var imageData = new byte[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentNullException>("fileName", async () =>
            await testClient.EditImageAsync(imageData, null!, "prompt"));
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullPrompt_Throws()
    {
        using var testClient = new TestImageClient();
        var imageData = new byte[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentNullException>("prompt", async () =>
            await testClient.EditImageAsync(imageData, "test.png", null!));
    }

    [Theory]
    [InlineData("test.png", "image/png")]
    [InlineData("test.jpg", "image/jpeg")]
    [InlineData("test.jpeg", "image/jpeg")]
    [InlineData("test.webp", "image/webp")]
    [InlineData("test.gif", "image/gif")]
    [InlineData("test.bmp", "image/bmp")]
    [InlineData("test.tiff", "image/tiff")]
    [InlineData("test.tif", "image/tiff")]
    [InlineData("test.unknown", "image/png")] // Unknown extension defaults to PNG
    [InlineData("TEST.PNG", "image/png")] // Case insensitive
    public async Task EditImageAsync_ByteArray_InfersCorrectMediaType(string fileName, string expectedMediaType)
    {
        // Arrange
        using var testClient = new TestImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var prompt = "Edit this image";

        testClient.GenerateImagesAsyncCallback = (request, o, ct) =>
        {
            Assert.NotNull(request.OriginalImages);
            var dataContent = Assert.IsType<DataContent>(Assert.Single(request.OriginalImages));
            Assert.Equal(expectedMediaType, dataContent.MediaType);
            return Task.FromResult(new ImageResponse());
        };

        // Act & Assert
        await testClient.EditImageAsync(imageData, fileName, prompt);
    }

    [Fact]
    public async Task EditImageAsync_AllMethods_PassDefaultOptionsAndCancellation()
    {
        // Arrange
        using var testClient = new TestImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var dataContent = new DataContent(imageData, "image/png");
        var prompt = "Edit this image";

        int callCount = 0;
        testClient.GenerateImagesAsyncCallback = (request, o, ct) =>
        {
            callCount++;
            Assert.Null(o); // Default options should be null
            Assert.Equal(CancellationToken.None, ct); // Default cancellation token
            Assert.NotNull(request.OriginalImages); // Should have original images for editing
            return Task.FromResult(new ImageResponse());
        };

        // Act - Test all two overloads with default parameters
        await testClient.EditImageAsync(dataContent, prompt);
        await testClient.EditImageAsync(imageData, "test.png", prompt);

        // Assert
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GenerateStreamingImagesAsync_WithPrompt_CallsGenerateStreamingImagesAsync()
    {
        // Arrange
        using var testClient = new TestImageClient();
        var prompt = "A beautiful sunset";
        var options = new ImageOptions { Count = 2 };
        var expectedUpdate = new ImageResponseUpdate();
        var cancellationToken = new CancellationToken(canceled: false);

        testClient.GenerateStreamingImagesAsyncCallback = (request, o, ct) =>
        {
            Assert.NotNull(request);
            Assert.Equal(prompt, request.Prompt);
            Assert.Null(request.OriginalImages);
            Assert.Same(options, o);
            Assert.Equal(cancellationToken, ct);
            return YieldAsync(expectedUpdate);
        };

        // Act
        var updates = new List<ImageResponseUpdate>();
        await foreach (var update in testClient.GenerateStreamingImagesAsync(prompt, options, cancellationToken))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Single(updates);
        Assert.Same(expectedUpdate, updates[0]);
    }

    [Fact]
    public async Task GenerateStreamingImagesAsync_NullClient_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>("client", async () =>
        {
            await foreach (var _ in ImageClientExtensions.GenerateStreamingImagesAsync(null!, "prompt"))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task GenerateStreamingImagesAsync_NullPrompt_Throws()
    {
        using var testClient = new TestImageClient();

        await Assert.ThrowsAsync<ArgumentNullException>("prompt", async () =>
        {
            await foreach (var _ in ImageClientExtensions.GenerateStreamingImagesAsync(testClient, null!))
            {
                // Should not reach here
            }
        });
    }

    private static async IAsyncEnumerable<T> YieldAsync<T>(T item)
    {
        // Helper method to yield an item asynchronously
        await Task.Yield();
        yield return item;
    }
}
