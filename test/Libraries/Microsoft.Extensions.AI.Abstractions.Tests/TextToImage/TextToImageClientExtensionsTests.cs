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
    public async Task EditImageAsync_DataContent_CallsEditImagesAsync()
    {
        // Arrange
        using var testClient = new TestTextToImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var dataContent = new DataContent(imageData, "image/png") { Name = "test.png" };
        var prompt = "Edit this image";
        var options = new TextToImageOptions { Count = 2 };
        var expectedResponse = new TextToImageResponse();
        var cancellationToken = new CancellationToken(canceled: false);

        testClient.EditImagesAsyncCallback = (originalImages, p, o, ct) =>
        {
            Assert.Single(originalImages);
            Assert.Same(dataContent, Assert.Single(originalImages));
            Assert.Equal(prompt, p);
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
        using var testClient = new TestTextToImageClient();
        var dataContent = new DataContent(new byte[] { 1, 2, 3 }, "image/png");

        await Assert.ThrowsAsync<ArgumentNullException>("client", async () =>
            await TextToImageClientExtensions.EditImageAsync(null!, dataContent, "prompt"));

        await Assert.ThrowsAsync<ArgumentNullException>("originalImage", async () =>
            await testClient.EditImageAsync(null!, "prompt"));

        await Assert.ThrowsAsync<ArgumentNullException>("prompt", async () =>
            await testClient.EditImageAsync(dataContent, null!));
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_CallsEditImagesAsync()
    {
        // Arrange
        using var testClient = new TestTextToImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var fileName = "test.jpg";
        var prompt = "Edit this image";
        var options = new TextToImageOptions { Count = 2 };
        var expectedResponse = new TextToImageResponse();
        var cancellationToken = new CancellationToken(canceled: false);

        testClient.EditImagesAsyncCallback = (originalImages, p, o, ct) =>
        {
            Assert.Single(originalImages);
            var dataContent = Assert.IsType<DataContent>(Assert.Single(originalImages));
            Assert.Equal(imageData, dataContent.Data.ToArray());
            Assert.Equal("image/jpeg", dataContent.MediaType);
            Assert.Equal(fileName, dataContent.Name);
            Assert.Equal(prompt, p);
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
            await TextToImageClientExtensions.EditImageAsync(null!, imageData, "test.png", "prompt"));
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullData_Throws()
    {
        using var testClient = new TestTextToImageClient();

        await Assert.ThrowsAsync<ArgumentNullException>("originalImageData", async () =>
        {
            byte[] nullData = null!;
            await testClient.EditImageAsync(nullData, "test.png", "prompt");
        });
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullFileName_Throws()
    {
        using var testClient = new TestTextToImageClient();
        var imageData = new byte[] { 1, 2, 3 };

        await Assert.ThrowsAsync<ArgumentNullException>("fileName", async () =>
            await testClient.EditImageAsync(imageData, null!, "prompt"));
    }

    [Fact]
    public async Task EditImageAsync_ByteArray_NullPrompt_Throws()
    {
        using var testClient = new TestTextToImageClient();
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
        using var testClient = new TestTextToImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var prompt = "Edit this image";

        testClient.EditImagesAsyncCallback = (originalImages, p, o, ct) =>
        {
            var dataContent = Assert.IsType<DataContent>(Assert.Single(originalImages));
            Assert.Equal(expectedMediaType, dataContent.MediaType);
            return Task.FromResult(new TextToImageResponse());
        };

        // Act & Assert
        await testClient.EditImageAsync(imageData, fileName, prompt);
    }

    [Fact]
    public async Task EditImageAsync_AllMethods_PassDefaultOptionsAndCancellation()
    {
        // Arrange
        using var testClient = new TestTextToImageClient();
        var imageData = new byte[] { 1, 2, 3, 4 };
        var dataContent = new DataContent(imageData, "image/png");
        using var stream = new MemoryStream(imageData);
        var prompt = "Edit this image";

        int callCount = 0;
        testClient.EditImagesAsyncCallback = (originalImages, p, o, ct) =>
        {
            callCount++;
            Assert.Null(o); // Default options should be null
            Assert.Equal(CancellationToken.None, ct); // Default cancellation token
            return Task.FromResult(new TextToImageResponse());
        };

        // Act - Test all three overloads with default parameters
        await testClient.EditImageAsync(dataContent, prompt);
        await testClient.EditImageAsync(imageData, "test.png", prompt);

        // Assert
        Assert.Equal(2, callCount);
    }
}
