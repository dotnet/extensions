// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingAudioTranscriptionClientTests
{
    [Fact]
    public void RequiresInnerChatClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingAudioTranscriptionClient(null!));
    }

    [Fact]
    public async Task TranscribeAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedContents = new List<IAsyncEnumerable<DataContent>>();
        var expectedOptions = new AudioTranscriptionOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<AudioTranscriptionResponse>();
        var expectedResponse = new AudioTranscriptionResponse([]);
        using var inner = new TestAudioTranscriptionClient
        {
            TranscribeAsyncCallback = (audioContents, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, audioContents);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingAudioTranscriptionClient(inner);

        // Act
        var resultTask = delegating.TranscribeAsync(expectedContents, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public async Task ChatStreamingAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedContents = new List<IAsyncEnumerable<DataContent>>();
        var expectedOptions = new AudioTranscriptionOptions();
        var expectedCancellationToken = CancellationToken.None;
        AudioTranscriptionResponseUpdate[] expectedResults =
        [
            new() { Text = "Transcription update 1" },
            new() { Text = "Transcription update 2" }
        ];

        using var inner = new TestAudioTranscriptionClient
        {
            TranscribeStreamingAsyncCallback = (chatContents, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, chatContents);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedResults);
            }
        };

        using var delegating = new NoOpDelegatingAudioTranscriptionClient(inner);

        // Act
        var resultAsyncEnumerable = delegating.TranscribeStreamingAsync(expectedContents, expectedOptions, expectedCancellationToken);

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
        using var inner = new TestAudioTranscriptionClient();
        using var delegating = new NoOpDelegatingAudioTranscriptionClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestAudioTranscriptionClient();
        using var delegating = new NoOpDelegatingAudioTranscriptionClient(inner);

        // Act
        var client = delegating.GetService<DelegatingAudioTranscriptionClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedParam = new object();
        var expectedKey = new object();
        using var expectedResult = new TestAudioTranscriptionClient();
        using var inner = new TestAudioTranscriptionClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingAudioTranscriptionClient(inner);

        // Act
        var client = delegating.GetService<IAudioTranscriptionClient>(expectedKey);

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
        using var inner = new TestAudioTranscriptionClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingAudioTranscriptionClient(inner);

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

    private sealed class NoOpDelegatingAudioTranscriptionClient(IAudioTranscriptionClient innerClient)
        : DelegatingAudioTranscriptionClient(innerClient);
}
