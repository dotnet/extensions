// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    public async Task GetResponseAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedContents = new List<IAsyncEnumerable<DataContent>>();
        var expectedOptions = new SpeechToTextOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<SpeechToTextResponse>();
        var expectedResponse = new SpeechToTextResponse([]);
        using var inner = new TestSpeechToTextClient
        {
            GetResponseAsyncCallback = (speechContents, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContents);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

        // Act
        var resultTask = delegating.TranscribeAudioAsync(expectedContents, expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedResponse);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedResponse, await resultTask);
    }

    [Fact]
    public async Task GetStreamingAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedContents = new List<IAsyncEnumerable<DataContent>>();
        var expectedOptions = new SpeechToTextOptions();
        var expectedCancellationToken = CancellationToken.None;
        SpeechToTextResponseUpdate[] expectedResults =
        [
            new() { Text = "Text update 1" },
            new() { Text = "Text update 2" }
        ];

        using var inner = new TestSpeechToTextClient
        {
            GetStreamingResponseAsyncCallback = (speechContents, options, cancellationToken) =>
            {
                Assert.Same(expectedContents, speechContents);
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedResults);
            }
        };

        using var delegating = new NoOpDelegatingSpeechToTextClient(inner);

        // Act
        var resultAsyncEnumerable = delegating.TranscribeStreamingAudioAsync(expectedContents, expectedOptions, expectedCancellationToken);

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
