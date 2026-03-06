// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.AI;

public class LoggingRealtimeClientTests
{
    [Fact]
    public async Task LoggingRealtimeClient_InvalidArgs_Throws()
    {
        await using var innerSession = new TestRealtimeClientSession();
        using var innerClient = new TestRealtimeClient(innerSession);
        Assert.Throws<ArgumentNullException>("innerClient", () => new LoggingRealtimeClient(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingRealtimeClient(innerClient, null!));
    }

    [Fact]
    public async Task UseLogging_AvoidsInjectingNopSession()
    {
        await using var innerSession = new TestRealtimeClientSession();

        using var c1 = new TestRealtimeClient(innerSession);
        using var pipeline1 = c1.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build();
        await using var s1 = await pipeline1.CreateSessionAsync();
        Assert.Null(pipeline1.GetService(typeof(LoggingRealtimeClient)));
        Assert.Same(innerSession, s1.GetService(typeof(IRealtimeClientSession)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        using var c2 = new TestRealtimeClient(innerSession);
        using var pipeline2 = c2.AsBuilder().UseLogging(factory).Build();
        await using var s2 = await pipeline2.CreateSessionAsync();
        Assert.NotNull(pipeline2.GetService(typeof(LoggingRealtimeClient)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        using var c3 = new TestRealtimeClient(innerSession);
        using var pipeline3 = c3.AsBuilder().UseLogging().Build(services);
        await using var s3 = await pipeline3.CreateSessionAsync();
        Assert.NotNull(pipeline3.GetService(typeof(LoggingRealtimeClient)));
        using var c4 = new TestRealtimeClient(innerSession);
        using var pipeline4 = c4.AsBuilder().UseLogging(null).Build(services);
        await using var s4 = await pipeline4.CreateSessionAsync();
        Assert.NotNull(pipeline4.GetService(typeof(LoggingRealtimeClient)));
        using var c5 = new TestRealtimeClient(innerSession);
        using var pipeline5 = c5.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services);
        await using var s5 = await pipeline5.CreateSessionAsync();
        Assert.Null(pipeline5.GetService(typeof(LoggingRealtimeClient)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task SendAsync_SessionUpdateMessage_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        await using var innerSession = new TestRealtimeClientSession();

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);
        await using var session = await client.CreateSessionAsync();

        await session.SendAsync(new RealtimeClientSessionUpdateMessage(new RealtimeSessionOptions { Model = "test-model", Instructions = "Be helpful" }));

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("SendAsync invoked:", entry.Message),
                entry => Assert.Contains("SendAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("SendAsync invoked.", entry.Message),
                entry => Assert.Contains("SendAsync completed.", entry.Message));
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
    public async Task SendAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        await using var innerSession = new TestRealtimeClientSession
        {
            SendAsyncCallback = (message, cancellationToken) => Task.CompletedTask,
        };

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging()
            .Build(services);
        await using var session = await client.CreateSessionAsync();

        await session.SendAsync(new RealtimeClientMessage { MessageId = "test-event-123" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("SendAsync invoked:", entry.Message),
                entry => Assert.Contains("SendAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("SendAsync invoked.", entry.Message),
                entry => Assert.Contains("SendAsync completed.", entry.Message));
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
    public async Task GetStreamingResponseAsync_LogsMessagesReceived(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        await using var innerSession = new TestRealtimeClientSession
        {
            GetStreamingResponseAsyncCallback = (cancellationToken) => GetMessagesAsync()
        };

        static async IAsyncEnumerable<RealtimeServerMessage> GetMessagesAsync()
        {
            await Task.Yield();
            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.OutputTextDelta, MessageId = "event-1" };
            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.OutputAudioDelta, MessageId = "event-2" };
        }

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        await foreach (var message in session.GetStreamingResponseAsync())
        {
            // nop
        }

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("GetStreamingResponseAsync invoked.", entry.Message),
                entry => Assert.Contains("received server message:", entry.Message),
                entry => Assert.Contains("received server message:", entry.Message),
                entry => Assert.Contains("GetStreamingResponseAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("GetStreamingResponseAsync invoked.", entry.Message),
                entry => Assert.Contains("received server message.", entry.Message),
                entry => Assert.Contains("received server message.", entry.Message),
                entry => Assert.Contains("GetStreamingResponseAsync completed.", entry.Message));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Fact]
    public async Task SendAsync_SessionUpdateMessage_LogsCancellation()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var cts = new CancellationTokenSource();

        await using var innerSession = new TestRealtimeClientSession
        {
            SendAsyncCallback = (msg, cancellationToken) =>
            {
                throw new OperationCanceledException(cancellationToken);
            },
        };

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => session.SendAsync(new RealtimeClientSessionUpdateMessage(new RealtimeSessionOptions()), cts.Token));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("SendAsync invoked.", entry.Message),
            entry => Assert.Contains("SendAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task SendAsync_SessionUpdateMessage_LogsErrors()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        await using var innerSession = new TestRealtimeClientSession
        {
            SendAsyncCallback = (msg, cancellationToken) =>
            {
                throw new InvalidOperationException("Test error");
            },
        };

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SendAsync(new RealtimeClientSessionUpdateMessage(new RealtimeSessionOptions())));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("SendAsync invoked.", entry.Message),
            entry => Assert.True(entry.Message.Contains("SendAsync failed.") && entry.Level == LogLevel.Error));
    }

    [Fact]
    public async Task GetStreamingResponseAsync_LogsCancellation()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var cts = new CancellationTokenSource();

        await using var innerSession = new TestRealtimeClientSession
        {
            GetStreamingResponseAsyncCallback = (cancellationToken) => ThrowCancellationAsync(cancellationToken)
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ThrowCancellationAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new OperationCanceledException(cancellationToken);
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var message in session.GetStreamingResponseAsync(cts.Token))
            {
                // nop
            }
        });

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("GetStreamingResponseAsync invoked.", entry.Message),
            entry => Assert.Contains("GetStreamingResponseAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task GetStreamingResponseAsync_LogsErrors()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        await using var innerSession = new TestRealtimeClientSession
        {
            GetStreamingResponseAsyncCallback = (cancellationToken) => ThrowErrorAsync()
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ThrowErrorAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("Test error");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var message in session.GetStreamingResponseAsync())
            {
                // nop
            }
        });

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("GetStreamingResponseAsync invoked.", entry.Message),
            entry => Assert.True(entry.Message.Contains("GetStreamingResponseAsync failed.") && entry.Level == LogLevel.Error));
    }

    [Fact]
    public async Task GetService_ReturnsLoggingClientWhenRequested()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddFakeLogging());

        await using var innerSession = new TestRealtimeClientSession();

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        Assert.NotNull(client.GetService(typeof(LoggingRealtimeClient)));
        Assert.Same(session, session.GetService(typeof(IRealtimeClientSession)));
    }

    [Fact]
    public async Task SendAsync_LogsCancellation()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var cts = new CancellationTokenSource();

        await using var innerSession = new TestRealtimeClientSession
        {
            SendAsyncCallback = (message, cancellationToken) =>
            {
                throw new OperationCanceledException(cancellationToken);
            },
        };

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.SendAsync(new RealtimeClientMessage { MessageId = "evt_cancel" }, cts.Token));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("SendAsync invoked.", entry.Message),
            entry => Assert.Contains("SendAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task SendAsync_LogsErrors()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        await using var innerSession = new TestRealtimeClientSession
        {
            SendAsyncCallback = (message, cancellationToken) =>
            {
                throw new InvalidOperationException("Inject error");
            },
        };

        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = innerClient
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
        await using var session = await client.CreateSessionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.SendAsync(new RealtimeClientMessage()));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("SendAsync invoked.", entry.Message),
            entry => Assert.True(entry.Message.Contains("SendAsync failed.") && entry.Level == LogLevel.Error));
    }

    [Fact]
    public async Task JsonSerializerOptions_NullValue_Throws()
    {
        await using var innerSession = new TestRealtimeClientSession();
        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = new LoggingRealtimeClient(innerClient, NullLogger.Instance);

        Assert.Throws<ArgumentNullException>("value", () => client.JsonSerializerOptions = null!);
    }

    [Fact]
    public async Task JsonSerializerOptions_Roundtrip()
    {
        await using var innerSession = new TestRealtimeClientSession();
        using var innerClient = new TestRealtimeClient(innerSession);
        using var client = new LoggingRealtimeClient(innerClient, NullLogger.Instance);

        var customOptions = new System.Text.Json.JsonSerializerOptions();
        client.JsonSerializerOptions = customOptions;

        Assert.Same(customOptions, client.JsonSerializerOptions);
    }

    [Fact]
    public void UseLogging_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>("builder", () =>
            ((RealtimeClientBuilder)null!).UseLogging());
    }

    private sealed class TestRealtimeClient : IRealtimeClient
    {
        private readonly IRealtimeClientSession _session;

        public TestRealtimeClient(IRealtimeClientSession session)
        {
            _session = session;
        }

        public Task<IRealtimeClientSession> CreateSessionAsync(RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_session);

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : _session.GetService(serviceType, serviceKey);

        public void Dispose()
        {
        }
    }
}
