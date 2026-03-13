// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedConversationChatClientTests
{
    [Fact]
    public void GetService_IHostedConversationClient_ReturnsConversationClient()
    {
        // Arrange
        using var innerChatClient = new TestChatClient();
        using var conversationClient = new TestHostedConversationClient();
        using var client = new HostedConversationChatClient(innerChatClient, conversationClient);

        // Act
        var result = client.GetService(typeof(IHostedConversationClient));

        // Assert
        Assert.Same(conversationClient, result);
    }

    [Fact]
    public void GetService_OtherTypes_DelegatesToInner()
    {
        // Arrange
        using var innerChatClient = new TestChatClient();
        using var conversationClient = new TestHostedConversationClient();
        using var client = new HostedConversationChatClient(innerChatClient, conversationClient);

        // Act - ask for the inner chat client type
        var result = client.GetService(typeof(TestChatClient));

        // Assert - DelegatingChatClient base dispatches to inner
        Assert.Same(innerChatClient, result);
    }

    [Fact]
    public void GetService_WithServiceKey_DelegatesToInner()
    {
        // Arrange
        var serviceKey = new object();
        var expectedResult = new object();
        using var innerChatClient = new TestChatClient
        {
            GetServiceCallback = (type, key) => key == serviceKey ? expectedResult : null
        };
        using var conversationClient = new TestHostedConversationClient();
        using var client = new HostedConversationChatClient(innerChatClient, conversationClient);

        // Act - when a key is provided, it should delegate even for IHostedConversationClient
        var result = client.GetService(typeof(IHostedConversationClient), serviceKey);

        // Assert
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task GetResponseAsync_PassesThroughUnchanged()
    {
        // Arrange
        var expectedMessages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var expectedOptions = new ChatOptions();
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hi"));

        using var innerChatClient = new TestChatClient
        {
            GetResponseAsyncCallback = (messages, options, _) =>
            {
                Assert.Same(expectedMessages, messages);
                Assert.Same(expectedOptions, options);
                return Task.FromResult(expectedResponse);
            }
        };
        using var conversationClient = new TestHostedConversationClient();
        using var client = new HostedConversationChatClient(innerChatClient, conversationClient);

        // Act
        var response = await client.GetResponseAsync(expectedMessages, expectedOptions);

        // Assert
        Assert.Same(expectedResponse, response);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_PassesThroughUnchanged()
    {
        // Arrange
        var expectedUpdate = new ChatResponseUpdate(ChatRole.Assistant, "streaming");
        using var innerChatClient = new TestChatClient
        {
            GetStreamingResponseAsyncCallback = (_, _, _) => YieldAsync(expectedUpdate)
        };
        using var conversationClient = new TestHostedConversationClient();
        using var client = new HostedConversationChatClient(innerChatClient, conversationClient);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new(ChatRole.User, "test")]))
        {
            updates.Add(update);
        }

        // Assert
        Assert.Single(updates);
        Assert.Same(expectedUpdate, updates[0]);
    }

    [Fact]
    public void Constructor_NullInnerChatClient_Throws()
    {
        using var conversationClient = new TestHostedConversationClient();
        Assert.Throws<ArgumentNullException>("innerClient", () => new HostedConversationChatClient(null!, conversationClient));
    }

    [Fact]
    public void Constructor_NullConversationClient_Throws()
    {
        using var innerChatClient = new TestChatClient();
        Assert.Throws<ArgumentNullException>("hostedConversationClient", () => new HostedConversationChatClient(innerChatClient, null!));
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldAsync(params ChatResponseUpdate[] updates)
    {
        await Task.Yield();
        foreach (var update in updates)
        {
            yield return update;
        }
    }

    private sealed class TestHostedConversationClient : IHostedConversationClient
    {
        public Task<HostedConversation> CreateAsync(HostedConversationCreationOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HostedConversation { ConversationId = "test" });

        public Task<HostedConversation> GetAsync(string conversationId, CancellationToken cancellationToken = default)
            => Task.FromResult(new HostedConversation { ConversationId = conversationId });

        public Task DeleteAsync(string conversationId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddMessagesAsync(string conversationId, IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public IAsyncEnumerable<ChatMessage> GetMessagesAsync(string conversationId, CancellationToken cancellationToken = default)
            => EmptyAsync();

        private static async IAsyncEnumerable<ChatMessage> EmptyAsync()
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceType is not null && serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
