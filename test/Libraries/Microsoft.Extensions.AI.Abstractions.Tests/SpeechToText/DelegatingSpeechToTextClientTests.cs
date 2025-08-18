// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingSpeechToTextClientTests
{
    [Fact]
    public void RequiresInnerSpeechToTextClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingSpeechToTextClient(null!));
    }

    [Fact]
    public async Task GetTextAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        using var expectedAudioSpeechStream = new MemoryStream();
        var expectedOptions = new SpeechToTextOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<SpeechToTextResponse>();
        var expectedResponse = new SpeechToTextResponse([]);
        using var inner = new TestSpeechToTextClient
        {
            GetTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                Assert.Same(expectedAudioSpeechStream, audioSpeechStream);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

        // Act
        var resultTask = delegating.GetTextAsync(expectedAudioSpeechStream, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public async Task GetStreamingTextAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        using var expectedAudioSpeechStream = new MemoryStream();
        var expectedOptions = new SpeechToTextOptions();
        var expectedCancellationToken = CancellationToken.None;
        SpeechToTextResponseUpdate[] expectedResults =
        [
            new("Text update 1"),
            new("Text update 2")
        ];

        using var inner = new TestSpeechToTextClient
        {
            GetStreamingTextAsyncCallback = (audioSpeechStream, options, cancellationToken) =>
            {
                Assert.Same(expectedAudioSpeechStream, audioSpeechStream);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedResults);
            }
        };

        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

        // Act
        var resultAsyncEnumerable = delegating.GetStreamingTextAsync(expectedAudioSpeechStream, expectedOptions, expectedCancellationToken);

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
        using var inner = new TestSpeechToTextClient();
        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestSpeechToTextClient();
        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

        // Act
        var client = delegating.GetService<DelegatingSpeechToTextClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedParam = new object();
        var expectedKey = new object();
        using var expectedResult = new TestSpeechToTextClient();
        using var inner = new TestSpeechToTextClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

        // Act
        var client = delegating.GetService<ISpeechToTextClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedParam = new object();
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestSpeechToTextClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

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

    private sealed class NoOpDelegatingSpeechToTextClient(ISpeechToTextClient innerClient)
        : DelegatingSpeechToTextClient(innerClient);
}
