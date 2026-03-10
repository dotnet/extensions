// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingTextToSpeechClientTests
{
    [Fact]
    public void RequiresInnerTextToSpeechClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingTextToSpeechClient(null!));
    }

    [Fact]
    public async Task GetAudioAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedText = "Hello, world!";
        var expectedOptions = new TextToSpeechOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<TextToSpeechResponse>();
        var expectedResponse = new TextToSpeechResponse([]);
        using var inner = new TestTextToSpeechClient
        {
            GetAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                Assert.Equal(expectedText, text);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingTextToSpeechClient(inner);

        // Act
        var resultTask = delegating.GetAudioAsync(expectedText, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public async Task GetStreamingAudioAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedText = "Hello, world!";
        var expectedOptions = new TextToSpeechOptions();
        var expectedCancellationToken = CancellationToken.None;
        TextToSpeechResponseUpdate[] expectedResults =
        [
            new([new DataContent(new byte[] { 1, 2 }, "audio/mpeg")]),
            new([new DataContent(new byte[] { 3, 4 }, "audio/mpeg")])
        ];

        using var inner = new TestTextToSpeechClient
        {
            GetStreamingAudioAsyncCallback = (text, options, cancellationToken) =>
            {
                Assert.Equal(expectedText, text);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedResults);
            }
        };

        using var delegating = new NoOpDelegatingTextToSpeechClient(inner);

        // Act
        var resultAsyncEnumerable = delegating.GetStreamingAudioAsync(expectedText, expectedOptions, expectedCancellationToken);

        // Assert
        var enumerator = resultAsyncEnumerable.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(expectedResults[0], enumerator.Current);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(expectedResults[1], enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestTextToSpeechClient();
        using var delegating = new NoOpDelegatingTextToSpeechClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestTextToSpeechClient();
        using var delegating = new NoOpDelegatingTextToSpeechClient(inner);

        // Act
        var client = delegating.GetService<DelegatingTextToSpeechClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestTextToSpeechClient();
        using var inner = new TestTextToSpeechClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingTextToSpeechClient(inner);

        // Act
        var client = delegating.GetService<ITextToSpeechClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestTextToSpeechClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingTextToSpeechClient(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    private static async IAsyncEnumerable<T> YieldAsync<T>(IEnumerable<T> input)
    {
        await Task.Yield();
        foreach (var item in input)
        {
            yield return item;
        }
    }

    private sealed class NoOpDelegatingTextToSpeechClient(ITextToSpeechClient innerClient)
        : DelegatingTextToSpeechClient(innerClient);
}
