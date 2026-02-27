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

public class RealtimeSessionBuilderTests
{
    [Fact]
    public void Ctor_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSession", () => new RealtimeSessionBuilder((IRealtimeSession)null!));
    }

    [Fact]
    public void Ctor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSessionFactory", () => new RealtimeSessionBuilder((Func<IServiceProvider, IRealtimeSession>)null!));
    }

    [Fact]
    public void Build_WithNoMiddleware_ReturnsInnerSession()
    {
        using var inner = new TestRealtimeSession();
        var builder = new RealtimeSessionBuilder(inner);

        var result = builder.Build();
        Assert.Same(inner, result);
    }

    [Fact]
    public void Build_WithFactory_UsesFactory()
    {
        using var inner = new TestRealtimeSession();
        var builder = new RealtimeSessionBuilder(_ => inner);

        var result = builder.Build();
        Assert.Same(inner, result);
    }

    [Fact]
    public void Use_NullSessionFactory_Throws()
    {
        using var inner = new TestRealtimeSession();
        var builder = new RealtimeSessionBuilder(inner);

        Assert.Throws<ArgumentNullException>("sessionFactory", () => builder.Use((Func<IRealtimeSession, IRealtimeSession>)null!));
        Assert.Throws<ArgumentNullException>("sessionFactory", () => builder.Use((Func<IRealtimeSession, IServiceProvider, IRealtimeSession>)null!));
    }

    [Fact]
    public void Use_StreamingDelegate_NullFunc_Throws()
    {
        using var inner = new TestRealtimeSession();
        var builder = new RealtimeSessionBuilder(inner);

        Assert.Throws<ArgumentNullException>(
            "getStreamingResponseFunc",
            () => builder.Use((Func<IRealtimeSession, CancellationToken, IAsyncEnumerable<RealtimeServerMessage>>)null!));
    }

    [Fact]
    public void Build_PipelineOrder_FirstAddedIsOutermost()
    {
        var callOrder = new List<string>();
        using var inner = new TestRealtimeSession();

        var builder = new RealtimeSessionBuilder(inner);
        builder.Use(session => new OrderTrackingSession(session, "first", callOrder));
        builder.Use(session => new OrderTrackingSession(session, "second", callOrder));

        using var pipeline = builder.Build();

        // The outermost should be "first" (added first)
        var outermost = Assert.IsType<OrderTrackingSession>(pipeline);
        Assert.Equal("first", outermost.Name);

        var middle = Assert.IsType<OrderTrackingSession>(outermost.GetInner());
        Assert.Equal("second", middle.Name);

        Assert.Same(inner, middle.GetInner());
    }

    [Fact]
    public void Build_WithServiceProvider_PassesToFactory()
    {
        IServiceProvider? capturedServices = null;
        using var inner = new TestRealtimeSession();

        var builder = new RealtimeSessionBuilder(inner);
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
    public void Build_NullServiceProvider_UsesEmptyProvider()
    {
        IServiceProvider? capturedServices = null;
        using var inner = new TestRealtimeSession();

        var builder = new RealtimeSessionBuilder(inner);
        builder.Use((session, services) =>
        {
            capturedServices = services;
            return session;
        });

        builder.Build(null);

        Assert.NotNull(capturedServices);
    }

    [Fact]
    public void Use_ReturnsSameBuilder_ForChaining()
    {
        using var inner = new TestRealtimeSession();
        var builder = new RealtimeSessionBuilder(inner);

        var returned = builder.Use(s => s);
        Assert.Same(builder, returned);
    }

    [Fact]
    public async Task Use_WithStreamingDelegate_InterceptsStreaming()
    {
        var intercepted = false;
        using var inner = new TestRealtimeSession
        {
            GetStreamingResponseAsyncCallback = (ct) => YieldSingle(new RealtimeServerMessage { MessageId = "inner" }, ct),
        };

        var builder = new RealtimeSessionBuilder(inner);
        builder.Use((innerSession, ct) =>
        {
            intercepted = true;
            return innerSession.GetStreamingResponseAsync(ct);
        });

        using var pipeline = builder.Build();
        await foreach (var msg in pipeline.GetStreamingResponseAsync())
        {
            Assert.Equal("inner", msg.MessageId);
        }

        Assert.True(intercepted);
    }

    [Fact]
    public void AsBuilder_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerSession", () => ((IRealtimeSession)null!).AsBuilder());
    }

    [Fact]
    public void AsBuilder_ReturnsBuilder()
    {
        using var inner = new TestRealtimeSession();
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

    private sealed class OrderTrackingSession : DelegatingRealtimeSession
    {
        public string Name { get; }
        private readonly List<string> _callOrder;

        public OrderTrackingSession(IRealtimeSession inner, string name, List<string> callOrder)
            : base(inner)
        {
            Name = name;
            _callOrder = callOrder;
        }

        public IRealtimeSession GetInner() => InnerSession;

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
