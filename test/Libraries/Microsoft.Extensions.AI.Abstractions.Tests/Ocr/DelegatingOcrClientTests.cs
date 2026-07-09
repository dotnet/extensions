// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingOcrClientTests
{
    [Fact]
    public void RequiresInnerOcrClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingOcrClient(null!));
    }

    [Fact]
    public async Task ExtractAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        using var expectedDocument = new MemoryStream();
        var expectedMediaType = "application/pdf";
        var expectedOptions = new OcrOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<OcrResult>();
        var expectedResponse = new OcrResult([]);
        using var inner = new TestOcrClient
        {
            ExtractAsyncCallback = (document, mediaType, options, progress, cancellationToken) =>
            {
                Assert.Same(expectedDocument, document);
                Assert.Same(expectedMediaType, mediaType);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingOcrClient(inner);

        // Act
        var resultTask = delegating.ExtractAsync(expectedDocument, expectedMediaType, expectedOptions, null, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestOcrClient();
        using var delegating = new NoOpDelegatingOcrClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestOcrClient();
        using var delegating = new NoOpDelegatingOcrClient(inner);

        // Act
        var client = delegating.GetService<DelegatingOcrClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestOcrClient();
        using var inner = new TestOcrClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingOcrClient(inner);

        // Act
        var client = delegating.GetService<IOcrClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestOcrClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingOcrClient(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    private sealed class NoOpDelegatingOcrClient(IOcrClient innerClient)
        : DelegatingOcrClient(innerClient);
}
