// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Shared.Instruments;
using Microsoft.TestUtilities;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific tests")]
public class LinuxNetworkMetricsTests
{
    private readonly Mock<ITcpStateInfoProvider> _tcpStateInfoProvider = new();
    private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;
    private FakeTimeProvider _timeProvider;

    public LinuxNetworkMetricsTests()
    {
        _timeProvider = new FakeTimeProvider(_startTime);

        _tcpStateInfoProvider.Setup(p => p.GetIpV4TcpStateInfo()).Returns(new TcpStateInfo());
        _tcpStateInfoProvider.Setup(p => p.GetIpV6TcpStateInfo()).Returns(new TcpStateInfo());
    }

    [Fact]
    public void CreatesMeter_WithCorrectName()
    {
        using var meterFactory = new TestMeterFactory();
        _ = new LinuxNetworkMetrics(
            meterFactory,
            _tcpStateInfoProvider.Object,
            _timeProvider);

        var meter = meterFactory.Meters.Single();
        Assert.Equal(ResourceUtilizationInstruments.MeterName, meter.Name);
    }

    [Fact]
    public void GetTcpStateInfoWithRetry_SuccessfulCall_ReturnsState()
    {
        var expectedV4 = new TcpStateInfo { ClosedCount = 42 };
        var expectedV6 = new TcpStateInfo { EstabCount = 24 };
        _tcpStateInfoProvider.Setup(p => p.GetIpV4TcpStateInfo()).Returns(expectedV4);
        _tcpStateInfoProvider.Setup(p => p.GetIpV6TcpStateInfo()).Returns(expectedV6);

        var metrics = CreateMetrics();
        var measurements = metrics.GetMeasurements().ToList();

        Assert.Contains(measurements, m => HasTagWithValue(m, "network.type", "ipv4", 42));
        Assert.Contains(measurements, m => HasTagWithValue(m, "system.network.state", "close", 42));
        Assert.Contains(measurements, m => HasTagWithValue(m, "network.type", "ipv6", 24));
        Assert.Contains(measurements, m => HasTagWithValue(m, "system.network.state", "established", 24));
    }

    [Theory]
    [InlineData(typeof(FileNotFoundException))]
    [InlineData(typeof(DirectoryNotFoundException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public void GetTcpStateInfoWithRetry_Failure_SetsUnavailableAndReturnsDefault(Type exceptionType)
    {
        _tcpStateInfoProvider.Setup(p => p.GetIpV4TcpStateInfo()).Throws((Exception)Activator.CreateInstance(exceptionType)!);

        var metrics = CreateMetrics();
        var measurements = metrics.GetMeasurements().ToList();

        Assert.All(measurements.Take(11), m => Assert.Equal(0, m.Value));
    }

    [Fact]
    public void GetTcpStateInfoWithRetry_DuringRetryInterval_ReturnsDefault()
    {
        _tcpStateInfoProvider.SetupSequence(p => p.GetIpV4TcpStateInfo())
            .Throws(new FileNotFoundException())
            .Returns(new TcpStateInfo { ClosedCount = 123 });

        var metrics = CreateMetrics();
        var first = metrics.GetMeasurements().ToList();

        _timeProvider.Advance(TimeSpan.FromMinutes(2));
        var second = metrics.GetMeasurements().ToList();

        Assert.All(first.Take(11), m => Assert.Equal(0, m.Value));
        Assert.All(second.Take(11), m => Assert.Equal(0, m.Value));
        _tcpStateInfoProvider.Verify(p => p.GetIpV4TcpStateInfo(), Times.Once);
    }

    [Fact]
    public void GetTcpStateInfoWithRetry_AfterRetryInterval_ResetsUnavailableOnSuccess()
    {
        _tcpStateInfoProvider.SetupSequence(p => p.GetIpV4TcpStateInfo())
            .Throws(new FileNotFoundException())
            .Returns(new TcpStateInfo { ClosedCount = 99 });

        var metrics = CreateMetrics();
        var first = metrics.GetMeasurements().ToList();

        _timeProvider.Advance(TimeSpan.FromMinutes(6));
        var second = metrics.GetMeasurements().ToList();

        Assert.All(first.Take(11), m => Assert.Equal(0, m.Value));
        Assert.Equal(99, second[0].Value);
        Assert.Contains(second, m => HasTagWithValue(m, "network.type", "ipv4", 99));
        Assert.Contains(second, m => HasTagWithValue(m, "system.network.state", "close", 99));

        _tcpStateInfoProvider.Verify(p => p.GetIpV4TcpStateInfo(), Times.Exactly(2));
    }

    private static bool HasTagWithValue(Measurement<long> measurement, string tagKey, string tagValue, long expectedValue)
    {
        foreach (var tag in measurement.Tags)
        {
            if (tag.Key == tagKey && (string)tag.Value == tagValue)
            {
                return measurement.Value == expectedValue;
            }
        }

        return false;
    }

    private LinuxNetworkMetrics CreateMetrics()
    {
        using var meterFactory = new TestMeterFactory();
        return new LinuxNetworkMetrics(
            meterFactory,
            _tcpStateInfoProvider.Object,
            _timeProvider);
    }
    [Fact]
    public void GetTcpStateInfoWithRetry_Multithreaded_RetryIsThreadSafe()
    {
        // Arrange
        var callCount = 0;
        var lockObj = new object();
        _tcpStateInfoProvider.Setup(p => p.GetIpV4TcpStateInfo()).Returns(() =>
        {
            lock (lockObj)
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new FileNotFoundException();
                }
                return new TcpStateInfo { ClosedCount = 55 };
            }
        });

        var metrics = CreateMetrics();
        var results = new List<List<Measurement<long>>>();
        var threads = new Thread[5];

        // Act
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                // Each thread tries to get measurements
                results.Add(metrics.GetMeasurements().ToList());
            });
            threads[i].Start();
        }
        foreach (var t in threads)
        {
            t.Join();
        }

        // Advance time to after retry interval
        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        // All threads should have received default (0) values due to retry interval
        foreach (var measurementList in results)
        {
            Assert.All(measurementList.Take(11), m => Assert.Equal(0, m.Value));
        }

        // Now, after retry interval, the next call should succeed and return the new value
        var afterRetry = metrics.GetMeasurements().ToList();
        Assert.Equal(55, afterRetry[0].Value);
        Assert.Contains(afterRetry, m => HasTagWithValue(m, "network.type", "ipv4", 55));
        Assert.Contains(afterRetry, m => HasTagWithValue(m, "system.network.state", "close", 55));

        // Only two calls to GetIpV4TcpStateInfo should have been made (one fail, one after retry)
        _tcpStateInfoProvider.Verify(p => p.GetIpV4TcpStateInfo(), Times.Exactly(2));
    }
}
