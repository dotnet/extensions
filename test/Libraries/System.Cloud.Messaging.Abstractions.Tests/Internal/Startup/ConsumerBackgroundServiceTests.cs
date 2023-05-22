// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.Tests.Internal.Startup;

/// <summary>
/// Tests for <see cref="ConsumerBackgroundService"/>.
/// </summary>
public class ConsumerBackgroundServiceTests
{
    [Fact]
    public void StartHost_ReturnsCompletedTask()
    {
        var mockConsumer = new Mock<IMessageConsumer>();
        using var consumerBackgroundService = new ConsumerBackgroundService(mockConsumer.Object);

        var task = consumerBackgroundService.StartAsync(CancellationToken.None);
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task StopHostWithoutStart_ShouldNotCallConsumerStartMethod()
    {
        var mockConsumer = new Mock<IMessageConsumer>();
        using var consumerBackgroundService = new ConsumerBackgroundService(mockConsumer.Object);

        await consumerBackgroundService.StopAsync(CancellationToken.None);

        mockConsumer.Verify(x => x.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StopHostAfterStartingIt_ShouldCallConsumerMethods()
    {
        var mockConsumer = new Mock<IMessageConsumer>();
        using var consumerBackgroundService = new ConsumerBackgroundService(mockConsumer.Object);

        await consumerBackgroundService.StartAsync(CancellationToken.None);
        await consumerBackgroundService.StopAsync(CancellationToken.None);

        mockConsumer.Verify(x => x.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
