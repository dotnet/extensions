﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingChatClientTests
{
    [Fact]
    public void RequiresInnerChatClient()
    {
        Assert.Throws<ArgumentNullException>(() => new NoOpDelegatingChatClient(null!));
    }

    [Fact]
    public void MetadataDefaultsToInnerClient()
    {
        using var inner = new TestChatClient();
        using var delegating = new NoOpDelegatingChatClient(inner);

        Assert.Same(inner.Metadata, delegating.Metadata);
    }

    [Fact]
    public async Task ChatAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedChatContents = new List<ChatMessage>();
        var expectedChatOptions = new ChatOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<ChatCompletion>();
        var expectedCompletion = new ChatCompletion([]);
        using var inner = new TestChatClient
        {
            CompleteAsyncCallback = (chatContents, options, cancellationToken) =>
            {
                Assert.Same(expectedChatContents, chatContents);
                Assert.Same(expectedChatOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingChatClient(inner);

        // Act
        var resultTask = delegating.CompleteAsync(expectedChatContents, expectedChatOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedCompletion);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedCompletion, await resultTask);
    }

    [Fact]
    public async Task ChatStreamingAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedChatContents = new List<ChatMessage>();
        var expectedChatOptions = new ChatOptions();
        var expectedCancellationToken = CancellationToken.None;
        StreamingChatCompletionUpdate[] expectedResults =
        [
            new() { Role = ChatRole.User, Text = "Message 1" },
            new() { Role = ChatRole.User, Text = "Message 2" }
        ];

        using var inner = new TestChatClient
        {
            CompleteStreamingAsyncCallback = (chatContents, options, cancellationToken) =>
            {
                Assert.Same(expectedChatContents, chatContents);
                Assert.Same(expectedChatOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedResults);
            }
        };

        using var delegating = new NoOpDelegatingChatClient(inner);

        // Act
        var resultAsyncEnumerable = delegating.CompleteStreamingAsync(expectedChatContents, expectedChatOptions, expectedCancellationToken);

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
        using var inner = new TestChatClient();
        using var delegating = new NoOpDelegatingChatClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestChatClient();
        using var delegating = new NoOpDelegatingChatClient(inner);

        // Act
        var client = delegating.GetService<DelegatingChatClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedParam = new object();
        var expectedKey = new object();
        using var expectedResult = new TestChatClient();
        using var inner = new TestChatClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingChatClient(inner);

        // Act
        var client = delegating.GetService<IChatClient>(expectedKey);

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
        using var inner = new TestChatClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingChatClient(inner);

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

    private sealed class NoOpDelegatingChatClient(IChatClient innerClient)
        : DelegatingChatClient(innerClient);
}
