// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeClientBuilderTests
{
    [Fact]
    public void Ctor_NullClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new RealtimeClientBuilder((IRealtimeClient)null!));
    }

    [Fact]
    public void Ctor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClientFactory", () => new RealtimeClientBuilder((Func<IServiceProvider, IRealtimeClient>)null!));
    }

    [Fact]
    public void Build_WithNoMiddleware_ReturnsInnerClient()
    {
        using var inner = new TestRealtimeClient();
        var builder = new RealtimeClientBuilder(inner);

        var result = builder.Build();
        Assert.Same(inner, result);
    }

    [Fact]
    public void Build_WithFactory_UsesFactory()
    {
        using var inner = new TestRealtimeClient();
        var builder = new RealtimeClientBuilder(_ => inner);

        var result = builder.Build();
        Assert.Same(inner, result);
    }

    [Fact]
    public void Use_NullClientFactory_Throws()
    {
        using var inner = new TestRealtimeClient();
        var builder = new RealtimeClientBuilder(inner);

        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IRealtimeClient, IRealtimeClient>)null!));
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IRealtimeClient, IServiceProvider, IRealtimeClient>)null!));
    }

    [Fact]
    public void Build_PipelineOrder_FirstAddedIsOutermost()
    {
        var callOrder = new List<string>();
        using var inner = new TestRealtimeClient();

        var builder = new RealtimeClientBuilder(inner);
        builder.Use(client => new OrderTrackingClient(client, "first", callOrder));
        builder.Use(client => new OrderTrackingClient(client, "second", callOrder));

        using var pipeline = builder.Build();

        // The outermost should be "first" (added first)
        var outermost = Assert.IsType<OrderTrackingClient>(pipeline);
        Assert.Equal("first", outermost.Name);

        var middle = Assert.IsType<OrderTrackingClient>(outermost.GetInner());
        Assert.Equal("second", middle.Name);

        Assert.Same(inner, middle.GetInner());
    }

    [Fact]
    public void Build_WithServiceProvider_PassesToFactory()
    {
        IServiceProvider? capturedServices = null;
        using var inner = new TestRealtimeClient();

        var builder = new RealtimeClientBuilder(inner);
        builder.Use((client, services) =>
        {
            capturedServices = services;
            return client;
        });

        var services = new EmptyServiceProvider();
        builder.Build(services);

        Assert.Same(services, capturedServices);
    }

    [Fact]
    public void Build_NullServiceProvider_UsesEmptyProvider()
    {
        IServiceProvider? capturedServices = null;
        using var inner = new TestRealtimeClient();

        var builder = new RealtimeClientBuilder(inner);
        builder.Use((client, services) =>
        {
            capturedServices = services;
            return client;
        });

        builder.Build(null);

        Assert.NotNull(capturedServices);
    }

    [Fact]
    public void Use_ReturnsSameBuilder_ForChaining()
    {
        using var inner = new TestRealtimeClient();
        var builder = new RealtimeClientBuilder(inner);

        var returned = builder.Use(c => c);
        Assert.Same(builder, returned);
    }

    [Fact]
    public void AsBuilder_NullClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => ((IRealtimeClient)null!).AsBuilder());
    }

    [Fact]
    public void AsBuilder_ReturnsBuilder()
    {
        using var inner = new TestRealtimeClient();
        var builder = inner.AsBuilder();

        Assert.NotNull(builder);
        Assert.Same(inner, builder.Build());
    }

    private sealed class TestRealtimeClient : IRealtimeClient
    {
        public Task<IRealtimeClientSession> CreateSessionAsync(RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IRealtimeClientSession>(new TestRealtimeClientSession());

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class OrderTrackingClient : DelegatingRealtimeClient
    {
        public string Name { get; }
        private readonly List<string> _callOrder;

        public OrderTrackingClient(IRealtimeClient inner, string name, List<string> callOrder)
            : base(inner)
        {
            Name = name;
            _callOrder = callOrder;
        }

        public IRealtimeClient GetInner() => InnerClient;

        public override async Task<IRealtimeClientSession> CreateSessionAsync(
            RealtimeSessionOptions? options = null, CancellationToken cancellationToken = default)
        {
            _callOrder.Add(Name);
            return await base.CreateSessionAsync(options, cancellationToken);
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
