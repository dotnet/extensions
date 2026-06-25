// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = OcrClientExtensions.GetService<object>(null!);
        });
    }

    [Fact]
    public async Task GetTextAsync_InvalidArgs_Throws()
    {
        IOcrClient? client = null;
        var content = new DataContent("data:application/pdf;base64,AQIDBA==");
        var ex1 = await Assert.ThrowsAsync<ArgumentNullException>(() => OcrClientExtensions.GetTextAsync(client!, content));
        Assert.Equal("client", ex1.ParamName);

        using var testClient = new TestOcrClient();
        DataContent? nullContent = null;
        var ex2 = await Assert.ThrowsAsync<ArgumentNullException>(() => OcrClientExtensions.GetTextAsync(testClient, nullContent!));
        Assert.Equal("document", ex2.ParamName);
    }

    [Fact]
    public async Task GetTextAsync_DataContent_PassesStreamAndMediaTypeAsync()
    {
        // Arrange
        var expectedResponse = new OcrResult([new OcrPage(0, "hello")]);
        string? observedMediaType = null;
        byte[]? observedBytes = null;

        using var client = new TestOcrClient
        {
            GetTextAsyncCallback = async (document, mediaType, options, progress, cancellationToken) =>
            {
                observedMediaType = mediaType;
                using var ms = new MemoryStream();
                await document.CopyToAsync(ms, cancellationToken);
                observedBytes = ms.ToArray();
                return expectedResponse;
            }
        };

        // Act
        var result = await OcrClientExtensions.GetTextAsync(client, new DataContent("data:application/pdf;base64,AQIDBA=="));

        // Assert
        Assert.Same(expectedResponse, result);
        Assert.Equal("application/pdf", observedMediaType);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, observedBytes);
    }
}
