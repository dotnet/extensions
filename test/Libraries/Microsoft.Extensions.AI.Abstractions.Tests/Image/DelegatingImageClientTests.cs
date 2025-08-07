// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingImageClientTests
{
    [Fact]
    public void RequiresInnerImageClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingImageClient(null!));
    }

    [Fact]
    public async Task GenerateImagesAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedRequest = new ImageRequest("test prompt");
        var expectedOptions = new ImageOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<ImageResponse>();
        var expectedResponse = new ImageResponse();
        using var inner = new TestImageClient
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingImageClient(inner);

        // Act
        var resultTask = delegating.GenerateImagesAsync(expectedRequest, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestImageClient();
        using var delegating = new NoOpDelegatingImageClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestImageClient();
        using var delegating = new NoOpDelegatingImageClient(inner);

        // Act
        var client = delegating.GetService<DelegatingImageClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestImageClient();
        using var inner = new TestImageClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingImageClient(inner);

        // Act
        var client = delegating.GetService<IImageClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestImageClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingImageClient(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        using var inner = new TestImageClient();
        var delegating = new NoOpDelegatingImageClient(inner);
        Assert.False(inner.DisposeInvoked);

        delegating.Dispose();
        Assert.True(inner.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        using var inner = new TestImageClient();
        var delegating = new NoOpDelegatingImageClient(inner);

        delegating.Dispose();
        Assert.True(inner.DisposeInvoked);

        // Second dispose should not throw
#pragma warning disable S3966
        delegating.Dispose();
#pragma warning restore S3966
        Assert.True(inner.DisposeInvoked);
    }

    private sealed class NoOpDelegatingImageClient(IImageClient innerClient)
        : DelegatingImageClient(innerClient);
}
