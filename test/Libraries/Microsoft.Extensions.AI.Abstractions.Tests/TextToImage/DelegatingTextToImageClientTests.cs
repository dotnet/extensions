// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingTextToImageClientTests
{
    [Fact]
    public void RequiresInnerTextToImageClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingTextToImageClient(null!));
    }

    [Fact]
    public async Task GenerateImagesAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedPrompt = "test prompt";
        var expectedOptions = new TextToImageOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<TextToImageResponse>();
        var expectedResponse = new TextToImageResponse();
        using var inner = new TestTextToImageClient
        {
            GenerateImagesAsyncCallback = (prompt, options, cancellationToken) =>
            {
                Assert.Same(expectedPrompt, prompt);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingTextToImageClient(inner);

        // Act
        var resultTask = delegating.GenerateImagesAsync(expectedPrompt, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public async Task EditImagesAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        AIContent[] expectedImages = [new DataContent(Array.Empty<byte>(), "image/png")];
        var expectedPrompt = "edit prompt";
        var expectedOptions = new TextToImageOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<TextToImageResponse>();
        var expectedResponse = new TextToImageResponse();
        using var inner = new TestTextToImageClient
        {
            EditImagesAsyncCallback = (originalImages, prompt, options, cancellationToken) =>
            {
                Assert.Same(expectedImages, originalImages);
                Assert.Same(expectedPrompt, prompt);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingTextToImageClient(inner);

        // Act
        var resultTask = delegating.EditImagesAsync(expectedImages, expectedPrompt, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestTextToImageClient();
        using var delegating = new NoOpDelegatingTextToImageClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestTextToImageClient();
        using var delegating = new NoOpDelegatingTextToImageClient(inner);

        // Act
        var client = delegating.GetService<DelegatingTextToImageClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestTextToImageClient();
        using var inner = new TestTextToImageClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingTextToImageClient(inner);

        // Act
        var client = delegating.GetService<ITextToImageClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestTextToImageClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingTextToImageClient(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    [Fact]
    public void Dispose_SetsFlag()
    {
        using var inner = new TestTextToImageClient();
        var delegating = new NoOpDelegatingTextToImageClient(inner);
        Assert.False(inner.DisposeInvoked);

        delegating.Dispose();
        Assert.True(inner.DisposeInvoked);
    }

    [Fact]
    public void Dispose_MultipleCallsSafe()
    {
        using var inner = new TestTextToImageClient();
        var delegating = new NoOpDelegatingTextToImageClient(inner);

        delegating.Dispose();
        Assert.True(inner.DisposeInvoked);

        // Second dispose should not throw
#pragma warning disable S3966
        delegating.Dispose();
#pragma warning restore S3966
        Assert.True(inner.DisposeInvoked);
    }

    private sealed class NoOpDelegatingTextToImageClient(ITextToImageClient innerClient)
        : DelegatingTextToImageClient(innerClient);
}
