// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Connections.Test;

[Collection(nameof(StaticFakeClockExecution))]
public sealed class ConnectionTimeoutDelegateTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;

    public ConnectionTimeoutDelegateTests()
    {
        _fakeTimeProvider = new FakeTimeProvider();
    }

    [Fact]
    public async Task OnConnectionAsync_ShouldThrow_WhenNoFeatures()
    {
        var next = Mock.Of<ConnectionDelegate>();
        var connectionTimeoutDelegate = new ConnectionTimeoutDelegate(next, Options.Create(new ConnectionTimeoutOptions())) { TimeProvider = _fakeTimeProvider };

        var featureCollection = new FeatureCollection();
        var connectionContext = new Mock<ConnectionContext>();
        connectionContext
            .Setup(context => context.Features)
            .Returns(featureCollection);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            connectionTimeoutDelegate.OnConnectionAsync(connectionContext.Object));
    }

    [Fact]
    public async Task OnConnectionAsync_ShouldCallNext_WhenNotificationFeature()
    {
        var next = Mock.Of<ConnectionDelegate>();
        var connectionTimeoutDelegate = new ConnectionTimeoutDelegate(next, Options.Create(new ConnectionTimeoutOptions())) { TimeProvider = _fakeTimeProvider };

        var notificationFeature = new Mock<IConnectionLifetimeNotificationFeature>();
        var featureCollection = new FeatureCollection();
        featureCollection.Set(notificationFeature.Object);

        var connectionContext = new Mock<ConnectionContext>();
        connectionContext
            .Setup(context => context.Features)
            .Returns(featureCollection);

        await connectionTimeoutDelegate.OnConnectionAsync(connectionContext.Object);

        Mock.Get(next).Verify(c => c.Invoke(connectionContext.Object), Times.Exactly(1));
    }

    [Fact]
    public async Task OnConnectionAsync_ShouldCloseConnection_WhenTimeout()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        using var semaphore = new SemaphoreSlim(0, 1);
        bool requestedClose = false;

        var connectionContext = new Mock<ConnectionContext>();
        var next = new Mock<ConnectionDelegate>();
        next
            .Setup(c => c.Invoke(connectionContext.Object))
            .Returns(async () => Assert.True(await semaphore.WaitAsync(TimeSpan.FromSeconds(15), cts.Token)));

        var connectionOptions = Options.Create(new ConnectionTimeoutOptions { Timeout = TimeSpan.FromMilliseconds(10) });
        var connectionTimeoutDelegate = new ConnectionTimeoutDelegate(next.Object, connectionOptions);

        var notificationFeature = new Mock<IConnectionLifetimeNotificationFeature>();
        notificationFeature
            .Setup(feature => feature.RequestClose())
            .Callback(() =>
            {
                requestedClose = true;
                semaphore.Release();
            });

        var featureCollection = new FeatureCollection();
        featureCollection.Set(notificationFeature.Object);

        connectionContext
            .Setup(context => context.Features)
            .Returns(featureCollection);

        connectionContext
            .Setup(context => context.ConnectionClosed)
            .Returns(() => CancellationToken.None);

        var connectionTask = connectionTimeoutDelegate.OnConnectionAsync(connectionContext.Object);

        while (!requestedClose)
        {
            cts.Token.ThrowIfCancellationRequested();
            _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(10));
        }

        await connectionTask;

        Assert.True(requestedClose);
        next.Verify(c => c.Invoke(connectionContext.Object), Times.Exactly(1));
        notificationFeature.Verify(n => n.RequestClose(), Times.Exactly(1));
    }
}
