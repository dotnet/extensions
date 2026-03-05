// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class DelegatingRealtimeClientSessionTests
{
    [Fact]
    public void Ctor_NullInnerSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSession", () => new NoOpDelegatingRealtimeClientSession(null!));
    }

    [Fact]
    public async Task Options_DelegatesToInner()
    {
        var expectedOptions = new RealtimeSessionOptions { Model = "test-model" };
        await using var inner = new TestRealtimeClientSession { Options = expectedOptions };
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        Assert.Same(expectedOptions, delegating.Options);
    }

    [Fact]
    public async Task SendAsync_SessionUpdateMessage_DelegatesToInner()
    {
        var called = false;
        var sentOptions = new RealtimeSessionOptions { Instructions = "Be helpful" };
        await using var inner = new TestRealtimeClientSession
        {
            SendAsyncCallback = (msg, _) =>
            {
                var updateMsg = Assert.IsType<RealtimeClientSessionUpdateMessage>(msg);
                Assert.Same(sentOptions, updateMsg.Options);
                called = true;
                return Task.CompletedTask;
            },
        };
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        await delegating.SendAsync(new RealtimeClientSessionUpdateMessage(sentOptions));
        Assert.True(called);
    }

    [Fact]
    public async Task SendAsync_DelegatesToInner()
    {
        var called = false;
        var sentMessage = new RealtimeClientMessage { MessageId = "evt_001" };
        await using var inner = new TestRealtimeClientSession
        {
            SendAsyncCallback = (msg, _) =>
            {
                Assert.Same(sentMessage, msg);
                called = true;
                return Task.CompletedTask;
            },
        };
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        await delegating.SendAsync(sentMessage);
        Assert.True(called);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_DelegatesToInner()
    {
        var expected = new RealtimeServerMessage { Type = RealtimeServerMessageType.Error, MessageId = "evt_002" };
        await using var inner = new TestRealtimeClientSession
        {
            GetStreamingResponseAsyncCallback = (ct) => YieldSingle(expected, ct),
        };
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        var messages = new List<RealtimeServerMessage>();
        await foreach (var msg in delegating.GetStreamingResponseAsync())
        {
            messages.Add(msg);
        }

        Assert.Single(messages);
        Assert.Same(expected, messages[0]);
    }

    [Fact]
    public async Task GetService_ReturnsSelfForMatchingType()
    {
        await using var inner = new TestRealtimeClientSession();
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        Assert.Same(delegating, delegating.GetService(typeof(NoOpDelegatingRealtimeClientSession)));
        Assert.Same(delegating, delegating.GetService(typeof(DelegatingRealtimeClientSession)));
        Assert.Same(delegating, delegating.GetService(typeof(IRealtimeClientSession)));
    }

    [Fact]
    public async Task GetService_DelegatesToInnerForUnknownType()
    {
        await using var inner = new TestRealtimeClientSession();
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        // TestRealtimeClientSession returns itself for matching types
        Assert.Same(inner, delegating.GetService(typeof(TestRealtimeClientSession)));
        Assert.Null(delegating.GetService(typeof(string)));
    }

    [Fact]
    public async Task GetService_WithServiceKey_DelegatesToInner()
    {
        await using var inner = new TestRealtimeClientSession();
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        // With a non-null key, delegating should NOT return itself even for matching types
        Assert.Null(delegating.GetService(typeof(NoOpDelegatingRealtimeClientSession), "someKey"));
    }

    [Fact]
    public async Task GetService_NullServiceType_Throws()
    {
        await using var inner = new TestRealtimeClientSession();
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public async Task DisposeAsync_DisposesInner()
    {
        var disposed = false;
        await using var inner = new DisposableTestRealtimeClientSession(() => disposed = true);
        var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        await delegating.DisposeAsync();
        Assert.True(disposed);
    }

    [Fact]
    public async Task SendAsync_SessionUpdateMessage_FlowsCancellationToken()
    {
        CancellationToken capturedToken = default;
        using var cts = new CancellationTokenSource();
        var sentOptions = new RealtimeSessionOptions();

        await using var inner = new TestRealtimeClientSession
        {
            SendAsyncCallback = (msg, ct) =>
            {
                capturedToken = ct;
                return Task.CompletedTask;
            },
        };
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        await delegating.SendAsync(new RealtimeClientSessionUpdateMessage(sentOptions), cts.Token);
        Assert.Equal(cts.Token, capturedToken);
    }

    [Fact]
    public async Task SendAsync_FlowsCancellationToken()
    {
        CancellationToken capturedToken = default;
        using var cts = new CancellationTokenSource();
        var sentMessage = new RealtimeClientMessage();

        await using var inner = new TestRealtimeClientSession
        {
            SendAsyncCallback = (msg, ct) =>
            {
                capturedToken = ct;
                return Task.CompletedTask;
            },
        };
        await using var delegating = new NoOpDelegatingRealtimeClientSession(inner);

        await delegating.SendAsync(sentMessage, cts.Token);
        Assert.Equal(cts.Token, capturedToken);
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> YieldSingle(
        RealtimeServerMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        await Task.CompletedTask.ConfigureAwait(false);
        yield return message;
    }

    /// <summary>A concrete DelegatingRealtimeClientSession for testing (since the base class is abstract-ish with protected ctor).</summary>
    private sealed class NoOpDelegatingRealtimeClientSession : DelegatingRealtimeClientSession
    {
        public NoOpDelegatingRealtimeClientSession(IRealtimeClientSession innerSession)
            : base(innerSession)
        {
        }
    }

    /// <summary>A test session that tracks Dispose calls.</summary>
    private sealed class DisposableTestRealtimeClientSession : IRealtimeClientSession
    {
        private readonly Action _onDispose;

        public DisposableTestRealtimeClientSession(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public RealtimeSessionOptions? Options => null;

        public Task SendAsync(RealtimeClientMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
            CancellationToken cancellationToken = default) =>
            EmptyUpdatesServer(cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public ValueTask DisposeAsync()
        {
            _onDispose();
            return default;
        }

        private static async IAsyncEnumerable<RealtimeServerMessage> EmptyUpdatesServer(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }
}
