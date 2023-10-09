// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.TimeProvider.Testing;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Hosting.Testing.Test;

public class HostTerminatorServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ServiceCanceled_DoesNothing()
    {
        var logger = new FakeLogger<HostTerminatorService>();
        var hostMock = new Mock<IHost>(MockBehavior.Strict);
        var options = new FakeHostOptions();

        using var sut = new HostTerminatorService(hostMock.Object, options, logger) { TimeProvider = new FakeTimeProvider() };
        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        Assert.Empty(logger.Collector.GetSnapshot());
    }

    [Fact]
    public async Task ExecuteAsync_TimeToLiveUp_LogsAndDisposesHost()
    {
        var timeProvider = new FakeTimeProvider();
        var logger = new FakeLogger<HostTerminatorService>();
        var hostMock = new Mock<IHost>(MockBehavior.Strict);
        hostMock.Setup(x => x.Dispose());
        hostMock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var options = new FakeHostOptions();

        using var sut = new HostTerminatorService(hostMock.Object, options, logger) { TimeProvider = timeProvider };
        var task = RunProtectedExecuteAsync(sut, CancellationToken.None);
        timeProvider.Advance(options.TimeToLive);

        await task;

        Assert.Equal(
            "FakeHostOptions.TimeToLive set to 00:00:30 is up, disposing the host.",
            logger.LatestRecord.Message);
        hostMock.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_DebuggerAttached_DoesNothing()
    {
        var logger = new FakeLogger<HostTerminatorService>();
        var hostMock = new Mock<IHost>(MockBehavior.Strict);
        var options = new FakeHostOptions();

        using var sut = new HostTerminatorService(hostMock.Object, options, logger) { DebuggerAttached = true };
        await RunProtectedExecuteAsync(sut, CancellationToken.None);

        Assert.Equal(
            "Debugger is attached. The host won't be automatically disposed.",
            logger.LatestRecord.Message);
        hostMock.VerifyAll();
    }

    private static Task RunProtectedExecuteAsync(HostTerminatorService instance, CancellationToken cancellationToken)
    {
        var methodInfo = typeof(HostTerminatorService)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)methodInfo.Invoke(instance, new object[] { cancellationToken })!;
    }
}
