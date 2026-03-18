// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingHostedConversationClientTests
{
    [Fact]
    public void Constructor_InvalidArgs_Throws()
    {
        using var innerClient = new TestHostedConversationClient();
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingHostedConversationClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingHostedConversationClient(innerClient, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesWithoutError()
    {
        using var innerClient = new TestHostedConversationClient();
        using var client = new LoggingHostedConversationClient(innerClient, NullLogger.Instance);
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task CreateAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        // Arrange
        var collector = new FakeLogCollector();
        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using var innerClient = new TestHostedConversationClient
        {
            CreateAsyncCallback = (_, _) =>
                Task.FromResult(new HostedConversation { ConversationId = "conv-1" })
        };

        var builder = new HostedConversationClientBuilder(innerClient);
        builder.UseLogging(services.GetRequiredService<ILoggerFactory>());
        using var client = builder.Build(services);

        // Act
        await client.CreateAsync(new HostedConversationClientOptions());

        // Assert
        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.True(logs.Count >= 2);
            Assert.Contains(logs, e => e.Message.Contains("CreateAsync") && e.Message.Contains("invoked"));
            Assert.Contains(logs, e => e.Message.Contains("CreateAsync") && e.Message.Contains("completed"));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.True(logs.Count >= 2);
            Assert.Contains(logs, e => e.Message.Contains("CreateAsync") && e.Message.Contains("invoked"));
            Assert.Contains(logs, e => e.Message.Contains("CreateAsync") && e.Message.Contains("completed"));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GetAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        // Arrange
        var collector = new FakeLogCollector();
        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using var innerClient = new TestHostedConversationClient
        {
            GetAsyncCallback = (id, opts, _) =>
                Task.FromResult(new HostedConversation { ConversationId = id })
        };

        var builder = new HostedConversationClientBuilder(innerClient);
        builder.UseLogging(services.GetRequiredService<ILoggerFactory>());
        using var client = builder.Build(services);

        // Act
        await client.GetAsync("conv-42");

        // Assert
        var logs = collector.GetSnapshot();
        if (level <= LogLevel.Debug)
        {
            Assert.True(logs.Count >= 2);
            Assert.Contains(logs, e => e.Message.Contains("GetAsync") && e.Message.Contains("conv-42"));
            Assert.Contains(logs, e => e.Message.Contains("GetAsync") && e.Message.Contains("completed"));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task DeleteAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        // Arrange
        var collector = new FakeLogCollector();
        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using var innerClient = new TestHostedConversationClient
        {
            DeleteAsyncCallback = (_, opts, _) => Task.CompletedTask
        };

        var builder = new HostedConversationClientBuilder(innerClient);
        builder.UseLogging(services.GetRequiredService<ILoggerFactory>());
        using var client = builder.Build(services);

        // Act
        await client.DeleteAsync("conv-99");

        // Assert
        var logs = collector.GetSnapshot();
        if (level <= LogLevel.Debug)
        {
            Assert.True(logs.Count >= 2);
            Assert.Contains(logs, e => e.Message.Contains("DeleteAsync") && e.Message.Contains("conv-99"));
            Assert.Contains(logs, e => e.Message.Contains("DeleteAsync") && e.Message.Contains("completed"));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopClient()
    {
        using var innerClient = new TestHostedConversationClient();

        var builder = new HostedConversationClientBuilder(innerClient);
        builder.UseLogging(NullLoggerFactory.Instance);
        using var built = builder.Build();

        // When NullLoggerFactory is used, LoggingHostedConversationClient should be skipped
        Assert.Null(built.GetService(typeof(LoggingHostedConversationClient)));
    }

    private sealed class TestHostedConversationClient : IHostedConversationClient
    {
        public Func<HostedConversationClientOptions?, CancellationToken, Task<HostedConversation>>? CreateAsyncCallback { get; set; }
        public Func<string, HostedConversationClientOptions?, CancellationToken, Task<HostedConversation>>? GetAsyncCallback { get; set; }
        public Func<string, HostedConversationClientOptions?, CancellationToken, Task>? DeleteAsyncCallback { get; set; }

        public Task<HostedConversation> CreateAsync(HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => CreateAsyncCallback?.Invoke(options, cancellationToken) ?? Task.FromResult(new HostedConversation { ConversationId = "test" });

        public Task<HostedConversation> GetAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => GetAsyncCallback?.Invoke(conversationId, options, cancellationToken) ?? Task.FromResult(new HostedConversation { ConversationId = conversationId });

        public Task DeleteAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => DeleteAsyncCallback?.Invoke(conversationId, options, cancellationToken) ?? Task.CompletedTask;

        public Task AddMessagesAsync(string conversationId, IEnumerable<ChatMessage> messages, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public IAsyncEnumerable<ChatMessage> GetMessagesAsync(string conversationId, HostedConversationClientOptions? options = null, CancellationToken cancellationToken = default)
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
