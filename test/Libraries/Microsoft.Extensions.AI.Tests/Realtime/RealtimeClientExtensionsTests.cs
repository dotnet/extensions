// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Microsoft.Extensions.AI;

public class RealtimeClientExtensionsTests
{
    [Fact]
    public void GetService_NullClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () => ((IRealtimeClient)null!).GetService<IRealtimeClient>());
    }

    [Fact]
    public void GetService_ReturnsMatchingService()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetService<TestRealtimeClient>();
        Assert.Same(client, result);
    }

    [Fact]
    public void GetService_ReturnsNullForNonMatchingType()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetService<string>();
        Assert.Null(result);
    }

    [Fact]
    public void GetService_WithServiceKey_ReturnsNull()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetService<TestRealtimeClient>("someKey");
        Assert.Null(result);
    }

    [Fact]
    public void GetService_ReturnsInterfaceType()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetService<IRealtimeClient>();
        Assert.Same(client, result);
    }

    [Fact]
    public void GetRequiredService_NullClient_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () => ((IRealtimeClient)null!).GetRequiredService(typeof(string)));
        Assert.Throws<ArgumentNullException>("client", () => ((IRealtimeClient)null!).GetRequiredService<string>());
    }

    [Fact]
    public void GetRequiredService_NullServiceType_Throws()
    {
        using var client = new TestRealtimeClient();
        Assert.Throws<ArgumentNullException>("serviceType", () => client.GetRequiredService(null!));
    }

    [Fact]
    public void GetRequiredService_ReturnsMatchingService()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetRequiredService<TestRealtimeClient>();
        Assert.Same(client, result);
    }

    [Fact]
    public void GetRequiredService_ReturnsInterfaceType()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetRequiredService<IRealtimeClient>();
        Assert.Same(client, result);
    }

    [Fact]
    public void GetRequiredService_NonGeneric_ReturnsMatchingService()
    {
        using var client = new TestRealtimeClient();
        var result = client.GetRequiredService(typeof(TestRealtimeClient));
        Assert.Same(client, result);
    }

    [Fact]
    public void GetRequiredService_ThrowsForNonMatchingType()
    {
        using var client = new TestRealtimeClient();
        Assert.Throws<InvalidOperationException>(() => client.GetRequiredService<string>());
    }

    [Fact]
    public void GetRequiredService_NonGeneric_ThrowsForNonMatchingType()
    {
        using var client = new TestRealtimeClient();
        Assert.Throws<InvalidOperationException>(() => client.GetRequiredService(typeof(string)));
    }

    [Fact]
    public void GetRequiredService_WithServiceKey_ThrowsForNonMatchingKey()
    {
        using var client = new TestRealtimeClient();
        Assert.Throws<InvalidOperationException>(() => client.GetRequiredService<TestRealtimeClient>("someKey"));
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
}
