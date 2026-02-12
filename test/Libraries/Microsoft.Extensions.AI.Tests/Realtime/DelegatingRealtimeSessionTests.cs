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

public class DelegatingRealtimeSessionTests
{
    [Fact]
    public void Ctor_NullInnerSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSession", () => new NoOpDelegatingRealtimeSession(null!));
    }

    [Fact]
    public void Options_DelegatesToInner()
    {
        var expectedOptions = new RealtimeSessionOptions { Model = "test-model" };
        using var inner = new TestRealtimeSession { Options = expectedOptions };
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        Assert.Same(expectedOptions, delegating.Options);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToInner()
    {
        var called = false;
        var sentOptions = new RealtimeSessionOptions { Instructions = "Be helpful" };
        using var inner = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, _) =>
            {
                Assert.Same(sentOptions, options);
                called = true;
                return Task.CompletedTask;
            },
        };
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        await delegating.UpdateAsync(sentOptions);
        Assert.True(called);
    }

    [Fact]
    public async Task InjectClientMessageAsync_DelegatesToInner()
    {
        var called = false;
        var sentMessage = new RealtimeClientMessage { EventId = "evt_001" };
        using var inner = new TestRealtimeSession
        {
            InjectClientMessageAsyncCallback = (msg, _) =>
            {
                Assert.Same(sentMessage, msg);
                called = true;
                return Task.CompletedTask;
            },
        };
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        await delegating.InjectClientMessageAsync(sentMessage);
        Assert.True(called);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_DelegatesToInner()
    {
        var expected = new RealtimeServerMessage { Type = RealtimeServerMessageType.Error, EventId = "evt_002" };
        using var inner = new TestRealtimeSession
        {
            GetStreamingResponseAsyncCallback = (_, ct) => YieldSingle(expected, ct),
        };
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        var messages = new List<RealtimeServerMessage>();
        await foreach (var msg in delegating.GetStreamingResponseAsync(EmptyUpdates()))
        {
            messages.Add(msg);
        }

        Assert.Single(messages);
        Assert.Same(expected, messages[0]);
    }

    [Fact]
    public void GetService_ReturnsSelfForMatchingType()
    {
        using var inner = new TestRealtimeSession();
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        Assert.Same(delegating, delegating.GetService(typeof(NoOpDelegatingRealtimeSession)));
        Assert.Same(delegating, delegating.GetService(typeof(DelegatingRealtimeSession)));
        Assert.Same(delegating, delegating.GetService(typeof(IRealtimeSession)));
    }

    [Fact]
    public void GetService_DelegatesToInnerForUnknownType()
    {
        using var inner = new TestRealtimeSession();
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        // TestRealtimeSession returns itself for matching types
        Assert.Same(inner, delegating.GetService(typeof(TestRealtimeSession)));
        Assert.Null(delegating.GetService(typeof(string)));
    }

    [Fact]
    public void GetService_WithServiceKey_DelegatesToInner()
    {
        using var inner = new TestRealtimeSession();
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        // With a non-null key, delegating should NOT return itself even for matching types
        Assert.Null(delegating.GetService(typeof(NoOpDelegatingRealtimeSession), "someKey"));
    }

    [Fact]
    public void GetService_NullServiceType_Throws()
    {
        using var inner = new TestRealtimeSession();
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        Assert.Throws<ArgumentNullException>("serviceType", () => delegating.GetService(null!));
    }

    [Fact]
    public void Dispose_DisposesInner()
    {
        var disposed = false;
        using var inner = new DisposableTestRealtimeSession(() => disposed = true);
        var delegating = new NoOpDelegatingRealtimeSession(inner);

        delegating.Dispose();
        Assert.True(disposed);
    }

    [Fact]
    public async Task UpdateAsync_FlowsCancellationToken()
    {
        CancellationToken capturedToken = default;
        using var cts = new CancellationTokenSource();
        var sentOptions = new RealtimeSessionOptions();

        using var inner = new TestRealtimeSession
        {
            UpdateAsyncCallback = (options, ct) =>
            {
                capturedToken = ct;
                return Task.CompletedTask;
            },
        };
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        await delegating.UpdateAsync(sentOptions, cts.Token);
        Assert.Equal(cts.Token, capturedToken);
    }

    [Fact]
    public async Task InjectClientMessageAsync_FlowsCancellationToken()
    {
        CancellationToken capturedToken = default;
        using var cts = new CancellationTokenSource();
        var sentMessage = new RealtimeClientMessage();

        using var inner = new TestRealtimeSession
        {
            InjectClientMessageAsyncCallback = (msg, ct) =>
            {
                capturedToken = ct;
                return Task.CompletedTask;
            },
        };
        using var delegating = new NoOpDelegatingRealtimeSession(inner);

        await delegating.InjectClientMessageAsync(sentMessage, cts.Token);
        Assert.Equal(cts.Token, capturedToken);
    }

    private static async IAsyncEnumerable<RealtimeClientMessage> EmptyUpdates(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> YieldSingle(
        RealtimeServerMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        await Task.CompletedTask.ConfigureAwait(false);
        yield return message;
    }

    /// <summary>A concrete DelegatingRealtimeSession for testing (since the base class is abstract-ish with protected ctor).</summary>
    private sealed class NoOpDelegatingRealtimeSession : DelegatingRealtimeSession
    {
        public NoOpDelegatingRealtimeSession(IRealtimeSession innerSession)
            : base(innerSession)
        {
        }
    }

    /// <summary>A test session that tracks Dispose calls.</summary>
    private sealed class DisposableTestRealtimeSession : IRealtimeSession
    {
        private readonly Action _onDispose;

        public DisposableTestRealtimeSession(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public RealtimeSessionOptions? Options => null;

        public Task UpdateAsync(RealtimeSessionOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task InjectClientMessageAsync(RealtimeClientMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
            IAsyncEnumerable<RealtimeClientMessage> updates, CancellationToken cancellationToken = default) =>
            EmptyUpdatesServer(cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() => _onDispose();

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
