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

public class RealtimeClientSessionBuilderTests
{
    [Fact]
    public void Ctor_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSession", () => new RealtimeClientSessionBuilder((IRealtimeClientSession)null!));
    }

    [Fact]
    public void Ctor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSessionFactory", () => new RealtimeClientSessionBuilder((Func<IServiceProvider, IRealtimeClientSession>)null!));
    }

    [Fact]
    public async Task Build_WithNoMiddleware_ReturnsInnerSession()
    {
        await using var inner = new TestRealtimeClientSession();
        var builder = new RealtimeClientSessionBuilder(inner);

        var result = builder.Build();
        Assert.Same(inner, result);
    }

    [Fact]
    public async Task Build_WithFactory_UsesFactory()
    {
        await using var inner = new TestRealtimeClientSession();
        var builder = new RealtimeClientSessionBuilder(_ => inner);

        var result = builder.Build();
        Assert.Same(inner, result);
    }

    [Fact]
    public async Task Use_NullSessionFactory_Throws()
    {
        await using var inner = new TestRealtimeClientSession();
        var builder = new RealtimeClientSessionBuilder(inner);

        Assert.Throws<ArgumentNullException>("sessionFactory", () => builder.Use((Func<IRealtimeClientSession, IRealtimeClientSession>)null!));
        Assert.Throws<ArgumentNullException>("sessionFactory", () => builder.Use((Func<IRealtimeClientSession, IServiceProvider, IRealtimeClientSession>)null!));
    }

    [Fact]
    public async Task Use_StreamingDelegate_NullFunc_Throws()
    {
        await using var inner = new TestRealtimeClientSession();
        var builder = new RealtimeClientSessionBuilder(inner);

        Assert.Throws<ArgumentNullException>(
            "getStreamingResponseFunc",
            () => builder.Use((Func<IRealtimeClientSession, CancellationToken, IAsyncEnumerable<RealtimeServerMessage>>)null!));
    }

    [Fact]
    public async Task Build_PipelineOrder_FirstAddedIsOutermost()
    {
        var callOrder = new List<string>();
        await using var inner = new TestRealtimeClientSession();

        var builder = new RealtimeClientSessionBuilder(inner);
        builder.Use(session => new OrderTrackingClientSession(session, "first", callOrder));
        builder.Use(session => new OrderTrackingClientSession(session, "second", callOrder));

        await using var pipeline = builder.Build();

        // The outermost should be "first" (added first)
        var outermost = Assert.IsType<OrderTrackingClientSession>(pipeline);
        Assert.Equal("first", outermost.Name);

        var middle = Assert.IsType<OrderTrackingClientSession>(outermost.GetInner());
        Assert.Equal("second", middle.Name);

        Assert.Same(inner, middle.GetInner());
    }

    [Fact]
    public async Task Build_WithServiceProvider_PassesToFactory()
    {
        IServiceProvider? capturedServices = null;
        await using var inner = new TestRealtimeClientSession();

        var builder = new RealtimeClientSessionBuilder(inner);
        builder.Use((session, services) =>
        {
            capturedServices = services;
            return session;
        });

        var services = new EmptyServiceProvider();
        builder.Build(services);

        Assert.Same(services, capturedServices);
    }

    [Fact]
    public async Task Build_NullServiceProvider_UsesEmptyProvider()
    {
        IServiceProvider? capturedServices = null;
        await using var inner = new TestRealtimeClientSession();

        var builder = new RealtimeClientSessionBuilder(inner);
        builder.Use((session, services) =>
        {
            capturedServices = services;
            return session;
        });

        builder.Build(null);

        Assert.NotNull(capturedServices);
    }

    [Fact]
    public async Task Use_ReturnsSameBuilder_ForChaining()
    {
        await using var inner = new TestRealtimeClientSession();
        var builder = new RealtimeClientSessionBuilder(inner);

        var returned = builder.Use(s => s);
        Assert.Same(builder, returned);
    }

    [Fact]
    public async Task Use_WithStreamingDelegate_InterceptsStreaming()
    {
        var intercepted = false;
        await using var inner = new TestRealtimeClientSession
        {
            GetStreamingResponseAsyncCallback = (ct) => YieldSingle(new RealtimeServerMessage { MessageId = "inner" }, ct),
        };

        var builder = new RealtimeClientSessionBuilder(inner);
        builder.Use((innerSession, ct) =>
        {
            intercepted = true;
            return innerSession.GetStreamingResponseAsync(ct);
        });

        await using var pipeline = builder.Build();
        await foreach (var msg in pipeline.GetStreamingResponseAsync())
        {
            Assert.Equal("inner", msg.MessageId);
        }

        Assert.True(intercepted);
    }

    [Fact]
    public void AsBuilder_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSession", () => ((IRealtimeClientSession)null!).AsBuilder());
    }

    [Fact]
    public async Task AsBuilder_ReturnsBuilder()
    {
        await using var inner = new TestRealtimeClientSession();
        var builder = inner.AsBuilder();

        Assert.NotNull(builder);
        Assert.Same(inner, builder.Build());
    }

    private static async IAsyncEnumerable<RealtimeServerMessage> YieldSingle(
        RealtimeServerMessage message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        await Task.CompletedTask.ConfigureAwait(false);
        yield return message;
    }

    private sealed class OrderTrackingClientSession : DelegatingRealtimeClientSession
    {
        public string Name { get; }
        private readonly List<string> _callOrder;

        public OrderTrackingClientSession(IRealtimeClientSession inner, string name, List<string> callOrder)
            : base(inner)
        {
            Name = name;
            _callOrder = callOrder;
        }

        public IRealtimeClientSession GetInner() => InnerSession;

        public override async IAsyncEnumerable<RealtimeServerMessage> GetStreamingResponseAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _callOrder.Add(Name);
            await foreach (var msg in base.GetStreamingResponseAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return msg;
            }
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
