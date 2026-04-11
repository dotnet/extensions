// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedConversationClientBuilderTest
{
    [Fact]
    public void Build_WithSimpleInnerClient_Works()
    {
        // Arrange
        using var innerClient = new TestHostedConversationClient();
        var builder = new HostedConversationClientBuilder(innerClient);

        // Act
        var result = builder.Build();

        // Assert
        Assert.Same(innerClient, result);
    }

    [Fact]
    public void Use_AddsMiddlewareInCorrectOrder()
    {
        // Arrange
        using var innerClient = new TestHostedConversationClient();
        var builder = new HostedConversationClientBuilder(innerClient);

        builder.Use(next => new NamedDelegatingHostedConversationClient("First", next));
        builder.Use(next => new NamedDelegatingHostedConversationClient("Second", next));
        builder.Use(next => new NamedDelegatingHostedConversationClient("Third", next));

        // Act
        var first = (NamedDelegatingHostedConversationClient)builder.Build();

        // Assert - outermost is first added
        Assert.Equal("First", first.Name);
        var second = (NamedDelegatingHostedConversationClient)first.GetInnerClient();
        Assert.Equal("Second", second.Name);
        var third = (NamedDelegatingHostedConversationClient)second.GetInnerClient();
        Assert.Equal("Third", third.Name);
    }

    [Fact]
    public void Build_WithIServiceProvider_PassesServiceProvider()
    {
        // Arrange
        var expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        using var expectedInnerClient = new TestHostedConversationClient();
        using var expectedOuterClient = new TestHostedConversationClient();

        var builder = new HostedConversationClientBuilder(services =>
        {
            Assert.Same(expectedServiceProvider, services);
            return expectedInnerClient;
        });

        builder.Use((innerClient, serviceProvider) =>
        {
            Assert.Same(expectedServiceProvider, serviceProvider);
            Assert.Same(expectedInnerClient, innerClient);
            return expectedOuterClient;
        });

        // Act & Assert
        Assert.Same(expectedOuterClient, builder.Build(expectedServiceProvider));
    }

    [Fact]
    public void Constructor_NullInnerClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new HostedConversationClientBuilder((IHostedConversationClient)null!));
    }

    [Fact]
    public void Constructor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClientFactory", () => new HostedConversationClientBuilder((Func<IServiceProvider, IHostedConversationClient>)null!));
    }

    [Fact]
    public void Use_NullFactory_Throws()
    {
        using var innerClient = new TestHostedConversationClient();
        var builder = new HostedConversationClientBuilder(innerClient);
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IHostedConversationClient, IHostedConversationClient>)null!));
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IHostedConversationClient, IServiceProvider, IHostedConversationClient>)null!));
    }

    [Fact]
    public void Build_FactoryReturnsNull_Throws()
    {
        using var innerClient = new TestHostedConversationClient();
        var builder = new HostedConversationClientBuilder(innerClient);
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("entry at index 0", ex.Message);
    }

    private sealed class NamedDelegatingHostedConversationClient : DelegatingHostedConversationClient
    {
        public NamedDelegatingHostedConversationClient(string name, IHostedConversationClient innerClient)
            : base(innerClient)
        {
            Name = name;
        }

        public string Name { get; }

        public IHostedConversationClient GetInnerClient() => InnerClient;
    }

    private sealed class TestHostedConversationClient : IHostedConversationClient
    {
        public Task<HostedConversation> CreateAsync(HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HostedConversation { ConversationId = "test" });

        public Task<HostedConversation> GetAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new HostedConversation { ConversationId = conversationId });

        public Task DeleteAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddMessagesAsync(string conversationId, IEnumerable<ChatMessage> messages, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public IAsyncEnumerable<ChatMessage> GetMessagesAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => EmptyAsync();

        public IAsyncEnumerable<HostedConversation> ListConversationsAsync(HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => EmptyConversationsAsync();

        private static async IAsyncEnumerable<ChatMessage> EmptyAsync()
        {
            await Task.CompletedTask;
            yield break;
        }

        private static async IAsyncEnumerable<HostedConversation> EmptyConversationsAsync()
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
