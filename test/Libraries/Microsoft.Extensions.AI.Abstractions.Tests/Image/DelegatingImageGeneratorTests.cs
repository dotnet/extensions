// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingImageGeneratorTests
{
    [Fact]
    public void RequiresInnerImageGenerator()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new NoOpDelegatingImageGenerator(null!));
    }

    [Fact]
    public async Task GenerateImagesAsyncDefaultsToInnerGeneratorAsync()
    {
        // Arrange
        var expectedRequest = new ImageGenerationRequest("test prompt");
        var expectedOptions = new ImageGenerationOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<ImageGenerationResponse>();
        var expectedResponse = new ImageGenerationResponse();
        using var inner = new TestImageGenerator
        {
            GenerateImagesAsyncCallback = (request, options, cancellationToken) =>
            {
                Assert.Same(expectedRequest, request);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingImageGenerator(inner);

        // Act
        var resultTask = delegating.GenerateAsync(expectedRequest, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestImageGenerator();
        using var delegating = new NoOpDelegatingImageGenerator(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestImageGenerator();
        using var delegating = new NoOpDelegatingImageGenerator(inner);

        // Act
        var generator = delegating.GetService<DelegatingImageGenerator>();

        // Assert
        Assert.Same(delegating, generator);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestImageGenerator();
        using var inner = new TestImageGenerator
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingImageGenerator(inner);

        // Act
        var generator = delegating.GetService<IImageGenerator>(expectedKey);

        // Assert
        Assert.Same(expectedResult, generator);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestImageGenerator
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingImageGenerator(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        using var inner = new TestImageGenerator();
        var delegating = new NoOpDelegatingImageGenerator(inner);
        Assert.False(inner.DisposeInvoked);

        delegating.Dispose();
        Assert.True(inner.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        using var inner = new TestImageGenerator();
        var delegating = new NoOpDelegatingImageGenerator(inner);

        delegating.Dispose();
        Assert.True(inner.DisposeInvoked);

        // Second dispose should not throw
#pragma warning disable S3966
        delegating.Dispose();
#pragma warning restore S3966
        Assert.True(inner.DisposeInvoked);
    }

    private sealed class NoOpDelegatingImageGenerator(IImageGenerator innerGenerator)
        : DelegatingImageGenerator(innerGenerator);
}
