// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ConfigureOptionsHostedConversationClientTests
{
    [Fact]
    public async Task ConfigureOptions_CallbackIsCalled()
    {
        // Arrange
        var callbackInvoked = false;
        HostedConversationCreationOptions? receivedByInner = null;

        using var innerClient = new TestHostedConversationClient
        {
            CreateAsyncCallback = (options, _) =>
            {
                receivedByInner = options;
                return Task.FromResult(new HostedConversation { ConversationId = "conv-1" });
            }
        };

        using var client = new ConfigureOptionsHostedConversationClient(innerClient, options =>
        {
            callbackInvoked = true;
            options.Metadata ??= new();
            options.Metadata["configured"] = "true";
        });

        // Act
        await client.CreateAsync(new HostedConversationCreationOptions());

        // Assert
        Assert.True(callbackInvoked);
        Assert.NotNull(receivedByInner);
        Assert.Equal("true", receivedByInner!.Metadata!["configured"]);
    }

    [Fact]
    public async Task ConfigureOptions_OptionsAreCloned_OriginalNotModified()
    {
        // Arrange
        HostedConversationCreationOptions? receivedByInner = null;
        var originalOptions = new HostedConversationCreationOptions
        {
            Metadata = new() { ["key"] = "original" }
        };

        using var innerClient = new TestHostedConversationClient
        {
            CreateAsyncCallback = (options, _) =>
            {
                receivedByInner = options;
                return Task.FromResult(new HostedConversation { ConversationId = "conv-1" });
            }
        };

        using var client = new ConfigureOptionsHostedConversationClient(innerClient, options =>
        {
            options.Metadata!["key"] = "modified";
        });

        // Act
        await client.CreateAsync(originalOptions);

        // Assert - original should not be modified
        Assert.Equal("original", originalOptions.Metadata["key"]);

        // Assert - inner received modified clone
        Assert.NotNull(receivedByInner);
        Assert.NotSame(originalOptions, receivedByInner);
        Assert.Equal("modified", receivedByInner!.Metadata!["key"]);
    }

    [Fact]
    public async Task ConfigureOptions_NullOptions_CreatesNew()
    {
        // Arrange
        HostedConversationCreationOptions? receivedByInner = null;

        using var innerClient = new TestHostedConversationClient
        {
            CreateAsyncCallback = (options, _) =>
            {
                receivedByInner = options;
                return Task.FromResult(new HostedConversation { ConversationId = "conv-1" });
            }
        };

        using var client = new ConfigureOptionsHostedConversationClient(innerClient, options =>
        {
            options.Metadata = new() { ["new"] = "value" };
        });

        // Act
        await client.CreateAsync(options: null);

        // Assert - a new options instance should have been created
        Assert.NotNull(receivedByInner);
        Assert.Equal("value", receivedByInner!.Metadata!["new"]);
    }

    [Fact]
    public void Constructor_NullCallback_Throws()
    {
        using var innerClient = new TestHostedConversationClient();
        Assert.Throws<ArgumentNullException>("configure", () => new ConfigureOptionsHostedConversationClient(innerClient, null!));
    }

    [Fact]
    public void Constructor_NullInnerClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ConfigureOptionsHostedConversationClient(null!, _ => { }));
    }

    private sealed class TestHostedConversationClient : IHostedConversationClient
    {
        public Func<HostedConversationCreationOptions?, CancellationToken, Task<HostedConversation>>? CreateAsyncCallback { get; set; }

        public Task<HostedConversation> CreateAsync(HostedConversationCreationOptions? options = null, CancellationToken cancellationToken = default)
            => CreateAsyncCallback?.Invoke(options, cancellationToken) ?? Task.FromResult(new HostedConversation { ConversationId = "test" });

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
