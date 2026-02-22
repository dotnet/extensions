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

public class LoggingRealtimeSessionTests
{
    [Fact]
    public void LoggingRealtimeSession_InvalidArgs_Throws()
    {
        using var innerSession = new TestRealtimeSession();
        Assert.Throws<ArgumentNullException>("innerSession", () => new LoggingRealtimeSession(null!, NullLogger.Instance));
        Assert.Throws<ArgumentNullException>("logger", () => new LoggingRealtimeSession(innerSession, null!));
    }

    [Fact]
    public void UseLogging_AvoidsInjectingNopSession()
    {
        using var innerSession = new TestRealtimeSession();

        Assert.Null(innerSession.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(LoggingRealtimeSession)));
        Assert.Same(innerSession, innerSession.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build().GetService(typeof(IRealtimeSession)));

        using var factory = LoggerFactory.Create(b => b.AddFakeLogging());
        Assert.NotNull(innerSession.AsBuilder().UseLogging(factory).Build().GetService(typeof(LoggingRealtimeSession)));

        ServiceCollection c = new();
        c.AddFakeLogging();
        var services = c.BuildServiceProvider();
        Assert.NotNull(innerSession.AsBuilder().UseLogging().Build(services).GetService(typeof(LoggingRealtimeSession)));
        Assert.NotNull(innerSession.AsBuilder().UseLogging(null).Build(services).GetService(typeof(LoggingRealtimeSession)));
        Assert.Null(innerSession.AsBuilder().UseLogging(NullLoggerFactory.Instance).Build(services).GetService(typeof(LoggingRealtimeSession)));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task UpdateAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using var innerSession = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, cancellationToken) => Task.CompletedTask,
        };

        using var session = innerSession
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await session.UpdateAsync(new RealtimeSessionOptions { Model = "test-model", Instructions = "Be helpful" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("UpdateAsync invoked:", entry.Message),
                entry => Assert.Contains("UpdateAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("UpdateAsync invoked.", entry.Message),
                entry => Assert.Contains("UpdateAsync completed.", entry.Message));
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
    public async Task InjectClientMessageAsync_LogsInvocationAndCompletion(LogLevel level)
    {
        var collector = new FakeLogCollector();

        ServiceCollection c = new();
        c.AddLogging(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));
        var services = c.BuildServiceProvider();

        using var innerSession = new TestRealtimeSession
        {
            InjectClientMessageAsyncCallback = (message, cancellationToken) => Task.CompletedTask,
        };

        using var session = innerSession
            .AsBuilder()
            .UseLogging()
            .Build(services);

        await session.InjectClientMessageAsync(new RealtimeClientMessage { MessageId = "test-event-123" });

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("InjectClientMessageAsync invoked:", entry.Message),
                entry => Assert.Contains("InjectClientMessageAsync completed.", entry.Message));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.Collection(logs,
                entry => Assert.Contains("InjectClientMessageAsync invoked.", entry.Message),
                entry => Assert.Contains("InjectClientMessageAsync completed.", entry.Message));
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

        using var innerSession = new TestRealtimeSession
        {
            GetStreamingResponseAsyncCallback = (messages, cancellationToken) => GetMessagesAsync()
        };

        static async IAsyncEnumerable<RealtimeServerMessage> GetMessagesAsync()
        {
            await Task.Yield();
            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.OutputTextDelta, MessageId = "event-1" };
            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.OutputAudioDelta, MessageId = "event-2" };
        }

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await foreach (var message in session.GetStreamingResponseAsync(EmptyAsyncEnumerableAsync()))
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
    public async Task UpdateAsync_LogsCancellation()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var cts = new CancellationTokenSource();

        using var innerSession = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, cancellationToken) =>
            {
                throw new OperationCanceledException(cancellationToken);
            },
        };

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => session.UpdateAsync(new RealtimeSessionOptions(), cts.Token));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("UpdateAsync invoked.", entry.Message),
            entry => Assert.Contains("UpdateAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task UpdateAsync_LogsErrors()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerSession = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, cancellationToken) =>
            {
                throw new InvalidOperationException("Test error");
            },
        };

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() => session.UpdateAsync(new RealtimeSessionOptions()));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("UpdateAsync invoked.", entry.Message),
            entry => Assert.True(entry.Message.Contains("UpdateAsync failed.") && entry.Level == LogLevel.Error));
    }

    [Fact]
    public async Task GetStreamingResponseAsync_LogsCancellation()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var cts = new CancellationTokenSource();

        using var innerSession = new TestRealtimeSession
        {
            GetStreamingResponseAsyncCallback = (messages, cancellationToken) => ThrowCancellationAsync(cancellationToken)
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ThrowCancellationAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new OperationCanceledException(cancellationToken);
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var message in session.GetStreamingResponseAsync(EmptyAsyncEnumerableAsync(), cts.Token))
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

        using var innerSession = new TestRealtimeSession
        {
            GetStreamingResponseAsyncCallback = (messages, cancellationToken) => ThrowErrorAsync()
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ThrowErrorAsync()
        {
            await Task.Yield();
            throw new InvalidOperationException("Test error");
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var message in session.GetStreamingResponseAsync(EmptyAsyncEnumerableAsync()))
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
    public void GetService_ReturnsLoggingSessionWhenRequested()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddFakeLogging());

        using var innerSession = new TestRealtimeSession();

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        Assert.NotNull(session.GetService(typeof(LoggingRealtimeSession)));
        Assert.Same(session, session.GetService(typeof(IRealtimeSession)));
    }

    [Fact]
    public async Task InjectClientMessageAsync_LogsCancellation()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var cts = new CancellationTokenSource();

        using var innerSession = new TestRealtimeSession
        {
            InjectClientMessageAsyncCallback = (message, cancellationToken) =>
            {
                throw new OperationCanceledException(cancellationToken);
            },
        };

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.InjectClientMessageAsync(new RealtimeClientMessage { MessageId = "evt_cancel" }, cts.Token));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("InjectClientMessageAsync invoked.", entry.Message),
            entry => Assert.Contains("InjectClientMessageAsync canceled.", entry.Message));
    }

    [Fact]
    public async Task InjectClientMessageAsync_LogsErrors()
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(LogLevel.Debug));

        using var innerSession = new TestRealtimeSession
        {
            InjectClientMessageAsyncCallback = (message, cancellationToken) =>
            {
                throw new InvalidOperationException("Inject error");
            },
        };

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.InjectClientMessageAsync(new RealtimeClientMessage()));

        var logs = collector.GetSnapshot();
        Assert.Collection(logs,
            entry => Assert.Contains("InjectClientMessageAsync invoked.", entry.Message),
            entry => Assert.True(entry.Message.Contains("InjectClientMessageAsync failed.") && entry.Level == LogLevel.Error));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    public async Task GetStreamingResponseAsync_LogsClientMessages(LogLevel level)
    {
        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)).SetMinimumLevel(level));

        using var innerSession = new TestRealtimeSession
        {
            GetStreamingResponseAsyncCallback = (updates, cancellationToken) => ConsumeAndYield(updates, cancellationToken)
        };

        static async IAsyncEnumerable<RealtimeServerMessage> ConsumeAndYield(
            IAsyncEnumerable<RealtimeClientMessage> updates, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                // consume
            }

            yield return new RealtimeServerMessage { Type = RealtimeServerMessageType.ResponseDone };
        }

        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory)
            .Build();

        var clientMessages = GetClientMessages();
        await foreach (var message in session.GetStreamingResponseAsync(clientMessages))
        {
            // consume
        }

        var logs = collector.GetSnapshot();
        if (level is LogLevel.Trace)
        {
            // Should log: invoked, client message (sensitive), server message (sensitive), completed
            Assert.True(logs.Count >= 3);
            Assert.Contains(logs, entry => entry.Message.Contains("GetStreamingResponseAsync invoked."));
            Assert.Contains(logs, entry => entry.Message.Contains("sending client message:"));
            Assert.Contains(logs, entry => entry.Message.Contains("GetStreamingResponseAsync completed."));
        }
        else if (level is LogLevel.Debug)
        {
            Assert.True(logs.Count >= 3);
            Assert.Contains(logs, entry => entry.Message.Contains("GetStreamingResponseAsync invoked."));
            Assert.Contains(logs, entry => entry.Message.Contains("sending client message."));
            Assert.Contains(logs, entry => entry.Message.Contains("GetStreamingResponseAsync completed."));
        }
        else
        {
            Assert.Empty(logs);
        }
    }

    [Fact]
    public void JsonSerializerOptions_NullValue_Throws()
    {
        using var innerSession = new TestRealtimeSession();
        using var session = new LoggingRealtimeSession(innerSession, NullLogger.Instance);

        Assert.Throws<ArgumentNullException>("value", () => session.JsonSerializerOptions = null!);
    }

    [Fact]
    public void JsonSerializerOptions_Roundtrip()
    {
        using var innerSession = new TestRealtimeSession();
        using var session = new LoggingRealtimeSession(innerSession, NullLogger.Instance);

        var customOptions = new System.Text.Json.JsonSerializerOptions();
        session.JsonSerializerOptions = customOptions;

        Assert.Same(customOptions, session.JsonSerializerOptions);
    }

    [Fact]
    public void UseLogging_NullBuilder_Throws()
    {
        Assert.Throws<ArgumentNullException>("builder", () =>
            ((RealtimeSessionBuilder)null!).UseLogging());
    }

    [Fact]
    public void UseLogging_ConfigureCallback_IsInvoked()
    {
        using var innerSession = new TestRealtimeSession();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddFakeLogging());

        bool configured = false;
        using var session = innerSession
            .AsBuilder()
            .UseLogging(loggerFactory, configure: s =>
            {
                configured = true;
            })
            .Build();

        Assert.True(configured);
    }

    private static async IAsyncEnumerable<RealtimeClientMessage> EmptyAsyncEnumerableAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    private static async IAsyncEnumerable<RealtimeClientMessage> GetClientMessages(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        await Task.CompletedTask.ConfigureAwait(false);
        yield return new RealtimeClientMessage { MessageId = "client_evt_1" };
    }
}
