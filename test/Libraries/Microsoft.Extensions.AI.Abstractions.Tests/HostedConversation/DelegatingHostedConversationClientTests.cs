// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DelegatingHostedConversationClientTests
{
    [Fact]
    public void RequiresInnerClient()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new NoOpDelegatingHostedConversationClient(null!));
    }

    [Fact]
    public async Task CreateAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedOptions = new HostedConversationClientOptions();
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<HostedConversation>();
        var expectedConversation = new HostedConversation { ConversationId = "conv-1" };
        using var inner = new TestHostedConversationClient
        {
            CreateAsyncCallback = (options, cancellationToken) =>
            {
                Assert.Same(expectedOptions, options);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var resultTask = delegating.CreateAsync(expectedOptions, expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedConversation);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedConversation, await resultTask);
    }

    [Fact]
    public async Task GetAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedConversationId = "conv-123";
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<HostedConversation>();
        var expectedConversation = new HostedConversation { ConversationId = expectedConversationId };
        using var inner = new TestHostedConversationClient
        {
            GetAsyncCallback = (conversationId, options, cancellationToken) =>
            {
                Assert.Equal(expectedConversationId, conversationId);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var resultTask = delegating.GetAsync(expectedConversationId, cancellationToken: expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(expectedConversation);
        Assert.True(resultTask.IsCompleted);
        Assert.Same(expectedConversation, await resultTask);
    }

    [Fact]
    public async Task DeleteAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedConversationId = "conv-123";
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<bool>();
        using var inner = new TestHostedConversationClient
        {
            DeleteAsyncCallback = (conversationId, options, cancellationToken) =>
            {
                Assert.Equal(expectedConversationId, conversationId);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var resultTask = delegating.DeleteAsync(expectedConversationId, cancellationToken: expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(true);
        Assert.True(resultTask.IsCompleted);
        await resultTask;
    }

    [Fact]
    public async Task AddMessagesAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedConversationId = "conv-123";
        var expectedMessages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var expectedCancellationToken = CancellationToken.None;
        var expectedResult = new TaskCompletionSource<bool>();
        using var inner = new TestHostedConversationClient
        {
            AddMessagesAsyncCallback = (conversationId, messages, options, cancellationToken) =>
            {
                Assert.Equal(expectedConversationId, conversationId);
                Assert.Same(expectedMessages, messages);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return expectedResult.Task;
            }
        };

        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var resultTask = delegating.AddMessagesAsync(expectedConversationId, expectedMessages, cancellationToken: expectedCancellationToken);

        // Assert
        Assert.False(resultTask.IsCompleted);
        expectedResult.SetResult(true);
        Assert.True(resultTask.IsCompleted);
        await resultTask;
    }

    [Fact]
    public async Task GetMessagesAsyncDefaultsToInnerClientAsync()
    {
        // Arrange
        var expectedConversationId = "conv-123";
        var expectedCancellationToken = CancellationToken.None;
        ChatMessage[] expectedMessages =
        [
            new(ChatRole.User, "Hello"),
            new(ChatRole.Assistant, "Hi"),
        ];

        using var inner = new TestHostedConversationClient
        {
            GetMessagesAsyncCallback = (conversationId, options, cancellationToken) =>
            {
                Assert.Equal(expectedConversationId, conversationId);
                Assert.Equal(expectedCancellationToken, cancellationToken);
                return YieldAsync(expectedMessages);
            }
        };

        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var resultAsyncEnumerable = delegating.GetMessagesAsync(expectedConversationId, cancellationToken: expectedCancellationToken);

        // Assert
        var enumerator = resultAsyncEnumerable.GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(expectedMessages[0], enumerator.Current);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(expectedMessages[1], enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public void GetServiceThrowsForNullType()
    {
        using var inner = new TestHostedConversationClient();
        using var delegating = new NoOpDelegatingHostedConversationClient(inner);
        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void GetServiceReturnsSelfIfCompatibleWithRequestAndKeyIsNull()
    {
        // Arrange
        using var inner = new TestHostedConversationClient();
        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var client = delegating.GetService<DelegatingHostedConversationClient>();

        // Assert
        Assert.Same(delegating, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfKeyIsNotNull()
    {
        // Arrange
        var expectedKey = new object();
        using var expectedResult = new TestHostedConversationClient();
        using var inner = new TestHostedConversationClient
        {
            GetServiceCallback = (_, _) => expectedResult
        };
        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var client = delegating.GetService<IHostedConversationClient>(expectedKey);

        // Assert
        Assert.Same(expectedResult, client);
    }

    [Fact]
    public void GetServiceDelegatesToInnerIfNotCompatibleWithRequest()
    {
        // Arrange
        var expectedResult = TimeZoneInfo.Local;
        var expectedKey = new object();
        using var inner = new TestHostedConversationClient
        {
            GetServiceCallback = (type, key) => type == expectedResult.GetType() && key == expectedKey
                ? expectedResult
                : throw new InvalidOperationException("Unexpected call")
        };
        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        // Act
        var tzi = delegating.GetService<TimeZoneInfo>(expectedKey);

        // Assert
        Assert.Same(expectedResult, tzi);
    }

    [Fact]
    public void DisposeDisposesInnerClient()
    {
        // Arrange
        using var inner = new TestHostedConversationClient();
        using var delegating = new NoOpDelegatingHostedConversationClient(inner);

        Assert.False(inner.Disposed);

        // Act
        delegating.Dispose();

        // Assert
        Assert.True(inner.Disposed);
    }

    private static async IAsyncEnumerable<T> YieldAsync<T>(IEnumerable<T> input)
    {
        await Task.Yield();
        foreach (var item in input)
        {
            yield return item;
        }
    }

    private sealed class NoOpDelegatingHostedConversationClient(IHostedConversationClient innerClient)
        : DelegatingHostedConversationClient(innerClient);

    private sealed class TestHostedConversationClient : IHostedConversationClient
    {
        public TestHostedConversationClient()
        {
            GetServiceCallback = DefaultGetServiceCallback;
        }

        public bool Disposed { get; private set; }

        public Func<HostedConversationClientOptions?, CancellationToken, Task<HostedConversation>>? CreateAsyncCallback { get; set; }

        public Func<string, HostedConversationClientOptions?, CancellationToken, Task<HostedConversation>>? GetAsyncCallback { get; set; }

        public Func<string, HostedConversationClientOptions?, CancellationToken, Task>? DeleteAsyncCallback { get; set; }

        public Func<string, IEnumerable<ChatMessage>, HostedConversationClientOptions?, CancellationToken, Task>? AddMessagesAsyncCallback { get; set; }

        public Func<string, HostedConversationClientOptions?, CancellationToken, IAsyncEnumerable<ChatMessage>>? GetMessagesAsyncCallback { get; set; }

        public Func<Type, object?, object?> GetServiceCallback { get; set; }

        private object? DefaultGetServiceCallback(Type serviceType, object? serviceKey) =>
            serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public Task<HostedConversation> CreateAsync(HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => CreateAsyncCallback!.Invoke(options, cancellationToken);

        public Task<HostedConversation> GetAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => GetAsyncCallback!.Invoke(conversationId, options, cancellationToken);

        public Task DeleteAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => DeleteAsyncCallback!.Invoke(conversationId, options, cancellationToken);

        public Task AddMessagesAsync(string conversationId, IEnumerable<ChatMessage> messages, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => AddMessagesAsyncCallback!.Invoke(conversationId, messages, options, cancellationToken);

        public IAsyncEnumerable<ChatMessage> GetMessagesAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => GetMessagesAsyncCallback!.Invoke(conversationId, options, cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null)
            => GetServiceCallback(serviceType, serviceKey);

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
