﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Providers;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Publishers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;
using static Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

/// <summary>
/// Tests for the DataTracker class.
/// </summary>
public sealed class ResourceUtilizationTrackerServiceTest
{
    private const string ProviderUnableToGatherData = "Unable to gather utilization statistics.";

    // We use this static fake clock in tests that doesn't advance the time.
    private static readonly FakeTimeProvider _clock = new();
    private static readonly string _publisherUnableToPublishErrorMessage = $"Publisher `{typeof(FaultPublisher).FullName}` was unable to publish utilization statistics.";

    /// <summary>
    /// Simply construct the object.
    /// </summary>
    /// <remarks>Tests that look into internals like this are evil.  Consider removing long term.</remarks>
    [Fact]
    public void BasicConstructor()
    {
        var mockProvider = new Mock<ISnapshotProvider>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<ResourceUtilizationTrackerService>>(MockBehavior.Loose);
        var publishersList = new List<IResourceUtilizationPublisher>
        {
            new EmptyPublisher(),
            new AnotherPublisher()
        };

        using var tracker = new ResourceUtilizationTrackerService(
            mockProvider.Object,
            mockLogger.Object,
            Create(new ResourceMonitoringOptions()),
            publishersList,
            _clock);
        var provider = GetDataTrackerField<ISnapshotProvider>(tracker, "_provider");
        var logger = GetDataTrackerField<ILogger<ResourceUtilizationTrackerService>>(tracker, "_logger");
        var publishers =
            GetDataTrackerField<IResourceUtilizationPublisher[]>(tracker, "_publishers");

        Assert.NotNull(provider);
        Assert.NotNull(logger);
        Assert.NotNull(publishers);
        Assert.Equal(2, publishers?.Length);
    }

    [Fact]
    public void BasicConstructor_NullOptions_Throws()
    {
        var mockProvider = new Mock<ISnapshotProvider>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<ResourceUtilizationTrackerService>>(MockBehavior.Loose);
        var mockPublishers = new Mock<IEnumerable<IResourceUtilizationPublisher>>(MockBehavior.Loose);

        Assert.Throws<ArgumentException>(() =>
            new ResourceUtilizationTrackerService(mockProvider.Object, mockLogger.Object, Create((ResourceMonitoringOptions)null!), mockPublishers.Object, _clock));
    }

    /// <summary>
    /// Simply construct the object (publisher constructor).
    /// </summary>
    /// <remarks>Tests that look into internals like this are evil.  Consider removing long term.</remarks>
    [Fact]
    public void BasicConstructor_NullPublishers_Throws()
    {
        var mockProvider = new Mock<ISnapshotProvider>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<ResourceUtilizationTrackerService>>(MockBehavior.Loose);

        Assert.Throws<ArgumentNullException>(() =>
            new ResourceUtilizationTrackerService(mockProvider.Object, mockLogger.Object, Create(new ResourceMonitoringOptions()), null!, TimeProvider.System));
    }

    /// <summary>
    /// Construct the object configured with maximum allowed values of <see cref="ResourceMonitoringOptions.AveragingWindow"/> and
    /// <see cref="ResourceMonitoringOptions.NumberOfSamples"/>.
    /// </summary>
    [Fact]
    public void BasicConstructor_ConfiguredWithMaxValuesOfSamplingWindowAndSamplingPeriod_DoesNotThrow()
    {
        var mockProvider = new Mock<ISnapshotProvider>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<ResourceUtilizationTrackerService>>(MockBehavior.Loose);
        var publishersList = new List<IResourceUtilizationPublisher>
        {
            new EmptyPublisher(),
            new AnotherPublisher()
        };

        var exception = Record.Exception(() =>
        {
            using var tracker = new ResourceUtilizationTrackerService(
                mockProvider.Object,
                mockLogger.Object,
                Create(new ResourceMonitoringOptions
                {
                    CollectionWindow = TimeSpan.FromMilliseconds(int.MaxValue),
                    SamplingInterval = TimeSpan.FromMilliseconds(int.MaxValue)
                }),
                publishersList,
                _clock);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Simply construct the object (publisher constructor).
    /// </summary>
    /// <remarks>Tests that look into internals like this are evil.  Consider removing long term.</remarks>
    [Fact]
    public void BasicConstructor_WithTwoPublishers()
    {
        var mockProvider = new Mock<ISnapshotProvider>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<ResourceUtilizationTrackerService>>(MockBehavior.Loose);
        var publishersList = new List<IResourceUtilizationPublisher>
        {
            new EmptyPublisher(),
            new EmptyPublisher()
        };

        using var tracker = new ResourceUtilizationTrackerService(
            mockProvider.Object,
            mockLogger.Object,
            Create(new ResourceMonitoringOptions()),
            publishersList,
            _clock);
        Assert.NotNull(tracker);

        var provider = GetDataTrackerField<ISnapshotProvider>(tracker, "_provider");
        var publishers
            = GetDataTrackerField<IResourceUtilizationPublisher[]>(tracker, "_publishers");
        var logger = GetDataTrackerField<ILogger>(tracker, "_logger");

        Assert.NotNull(logger);
        Assert.NotNull(provider);
        Assert.NotNull(logger);
        Assert.NotNull(publishers);
        Assert.IsType<IResourceUtilizationPublisher[]>(publishers);
        Assert.Equal(2, publishers?.Length);
    }

    /// <summary>
    /// Simply construct the object (complex constructor, with counter publishing).
    /// </summary>
    /// <remarks>Tests that look into internals like this are evil.  Consider removing long term.</remarks>
    [Fact]
    public void BasicConstructor_Complex_WithEmptyPublisher()
    {
        var mockProvider = new Mock<ISnapshotProvider>(MockBehavior.Loose);
        var mockLogger = new Mock<ILogger<ResourceUtilizationTrackerService>>(MockBehavior.Loose);
        using var tracker = new ResourceUtilizationTrackerService(mockProvider.Object, mockLogger.Object,
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromSeconds(1),
                CalculationPeriod = TimeSpan.FromSeconds(1)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new EmptyPublisher()
            },
            _clock);

        var publishers = GetDataTrackerField<IResourceUtilizationPublisher[]>(tracker, "_publishers");

        Assert.NotNull(publishers);
        Assert.IsType<IResourceUtilizationPublisher[]>(publishers);
        Assert.Equal(1, publishers?.Length);
    }

    [Fact]
    public async Task StartAsync_WithSimulatingThatTimeDidNotPass_NoUtilizationDataWillBeGathered()
    {
        const int TimerPeriod = 100;
        var numberOfSnapshots = 0;
        var logger = new FakeLogger<ResourceUtilizationTrackerService>();
        var provider = new FakeProvider();

        using var tracker = new ResourceUtilizationTrackerService(
            provider,
            logger,

            // Here we set the number of options to 1 so the internal timer
            // period will equal the AverageWindow.
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromMilliseconds(TimerPeriod),
                SamplingInterval = TimeSpan.FromMilliseconds(TimerPeriod),
                CalculationPeriod = TimeSpan.FromMilliseconds(TimerPeriod)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new GenericPublisher(_ => numberOfSnapshots++)
            },
            _clock);

        // Start running the tracker.
        _ = tracker.StartAsync(CancellationToken.None);

        // waiting for 3 cycles of execution, however the tracker's timer won't
        // be triggered as we didn't advance time via the fake clock.
        await Task.Delay(TimeSpan.FromMilliseconds(TimerPeriod * 3));

        await tracker.StopAsync(CancellationToken.None);

        // Since we did not advance the clock, then time did not pass. So, the timer should remain idle.
        Assert.Equal(0, numberOfSnapshots);
    }

    [Fact]
    public async Task RunTrackerAsync_IfProviderThrows_LogsError()
    {
        var clock = new FakeTimeProvider();
        var logger = new FakeLogger<ResourceUtilizationTrackerService>();
        var provider = new FaultProvider
        {
            // prevent the provider from throwing exception just to not fail the test
            // while initializing the tracker.
            ShouldThrow = false
        };
        using var e = new ManualResetEventSlim();

        using var tracker = new ResourceUtilizationTrackerService(
            provider,
            logger,
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromMilliseconds(100),
                CalculationPeriod = TimeSpan.FromMilliseconds(100),
                SamplingInterval = TimeSpan.FromMilliseconds(1)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new GenericPublisher((_) => e.Set())
            },
            clock);

        await tracker.StartAsync(CancellationToken.None);

        // Now, allow the faulted provider to throw.
        provider.ShouldThrow = true;

        clock.Advance(TimeSpan.FromMilliseconds(1));
        clock.Advance(TimeSpan.FromMilliseconds(1));

        e.Wait();

        Assert.Contains(ProviderUnableToGatherData, logger.Collector.LatestRecord.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunTrackerAsync_IfPublisherThrows_LogsError()
    {
        var logger = new FakeLogger<ResourceUtilizationTrackerService>();

        using var tracker = new ResourceUtilizationTrackerService(
            new FakeProvider(),
            logger,
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromMilliseconds(100),
                CalculationPeriod = TimeSpan.FromMilliseconds(100)
            }),
            new List<IResourceUtilizationPublisher>
            {
                // initialize with a publisher that throws an exception when trying to publish.
                new FaultPublisher()
            },
            _clock);

        // running the tracker logic for a single time without starting the tracker
        // service itself. By this we avoid starting the timer and the async behavior
        // and thus eliminate the flakiness introduced because of it.
        await tracker.PublishUtilizationAsync(CancellationToken.None);

        Assert.Contains(_publisherUnableToPublishErrorMessage, logger.Collector.LatestRecord.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validate that the tracker invokes the publisher's Publish method.
    /// </summary>
    /// <remarks>Tests that look into internals like this are evil.  Consider removing long term.</remarks>
    [Fact]
    public async Task ResourceUtilizationTracker_InitializedProperly_InvokesPublishers()
    {
        const int TimerPeriod = 100;
        bool publisherCalled = false;

        // Here we use local clock to keep its state local to the test.
        var clock = new FakeTimeProvider();

        using var autoResetEvent = new AutoResetEvent(false);

        using var tracker = new ResourceUtilizationTrackerService(
            new FakeProvider(),
            new NullLogger<ResourceUtilizationTrackerService>(),

            // Here we set the number of options to 1 so the internal timer
            // period will equal the AverageWindow.
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromMilliseconds(TimerPeriod),
                CalculationPeriod = TimeSpan.FromMilliseconds(TimerPeriod),
                SamplingInterval = TimeSpan.FromMilliseconds(TimerPeriod)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new GenericPublisher(_ =>
                {
                    publisherCalled = true;
                    autoResetEvent.Set();
                })
            },
            clock);

        // Start running the tracker.
        await tracker.StartAsync(CancellationToken.None);

        do
        {
            // Advance the clock 100 milliseconds to simulate passing of time
            // and allow the tracker to execute one cycle of gathering utilization data.
            clock.Advance(TimeSpan.FromMilliseconds(TimerPeriod));
        }
        while (!autoResetEvent.WaitOne(1));

        await tracker.StopAsync(CancellationToken.None);

        // Asserts that the publisher was called.
        Assert.True(publisherCalled);
    }

    [Fact(Skip = "Broken test, see https://github.com/dotnet/r9/issues/404")]
    public async Task ResourceUtilizationTracker_WhenInitializedWithZeroSnapshots_ReportsHighCpuSpikesThenConvergeInFewCycles()
    {
        // This test shows that initializing the internal buffer of the tracker with snapshots
        // with zeros for the kernel time and user time will cause the first values of CPU
        // utilization to be very high in a way that don't reflect the change in CPU time.

        const int TimerPeriod = 100;

        var clock = new FakeTimeProvider();
        var zerosSnapshot = new ResourceUtilizationSnapshot(
            TimeSpan.FromTicks(clock.GetUtcNow().Ticks),
            TimeSpan.Zero,
            TimeSpan.Zero,
            0);

        // This array simulates a series of snapshot values where the CPU kernel time
        // and CPU user time are constant and don't change, with this series of snapshots
        // the tracker should emit 0% CPU all the time, however, because the tracker
        // internal buffer was initialized with zero values snapshot, we will find that
        // the produced CPU% will start with high values then with time it will converge
        // to 0%.
        var snapshotsSequence = new[]
        {
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 2).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 3).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 4).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 5).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200)
        };

        using var autoResetEvent = new AutoResetEvent(false);
        var provider = new FakeProvider();

        // Initially set the provider to return zero snapshot, and use this snapshot
        // to initialize the tracker's internal buffer with zero snapshots.
        provider.SetNextSnapshot(zerosSnapshot);

        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(TimerPeriod * 2),
            CalculationPeriod = TimeSpan.FromMilliseconds(TimerPeriod * 2),
            SamplingInterval = TimeSpan.FromMilliseconds(TimerPeriod)
        };

        using var tracker = new ResourceUtilizationTrackerService(
            provider,
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(options),
            new List<IResourceUtilizationPublisher>
            {
                new GenericPublisher(_ =>
                {
                    autoResetEvent.Set();
                }),
            },
            clock);

        // Start running the tracker.
        _ = tracker.StartAsync(CancellationToken.None);

        var cpuValuesWithHighSpikes = new int[5];

        // In the following cycles the CPU% will be reported with high
        // values (100%) then should reduce with each cycle until reach
        // the value of 0%
        for (int i = 0; i < snapshotsSequence.Length; i++)
        {
            provider.SetNextSnapshot(snapshotsSequence[i]);

            // Advance time.
            clock.Advance(TimeSpan.FromMilliseconds(TimerPeriod));

            // This is to allow the service code to execute until it gets blocked in Task.Delay, and then
            // make sure it gets unblocked from the Delay.
            while (!autoResetEvent.WaitOne(10))
            {
                clock.Advance(TimeSpan.FromTicks(1));
            }

            var utilization = tracker.GetUtilization(options.CollectionWindow);
            cpuValuesWithHighSpikes[i] = (int)utilization.CpuUsedPercentage;
        }

        // Stop the tracker.
        await tracker.StopAsync(CancellationToken.None);

        Assert.Equal(0, cpuValuesWithHighSpikes[cpuValuesWithHighSpikes.Length - 1]);
    }

    [Fact]
    public async Task ResourceUtilizationTracker_WhenInitializedWithProperSnapshots_ReportsNoHighCpuSpikes()
    {
        // This test shows that initializing the internal buffer of the tracker with snapshots
        // with normal values for the kernel time and user time will eliminate any high CPU
        // utilization spikes that may appear in the first readings from the tracker.

        const int TimerPeriod = 100;

        var clock = new FakeTimeProvider();
        var properInitSnapshot = new ResourceUtilizationSnapshot(
            TimeSpan.FromTicks(clock.GetUtcNow().Ticks),
            TimeSpan.FromMilliseconds(250),
            TimeSpan.FromMilliseconds(250),
            200);

        // This array simulates a series of snapshot values where the CPU kernel time
        // and CPU user time are constant and don't change, with this series of snapshots
        // the tracker should emit 0% CPU all the time. Since in this test the tracker
        // is initialized with proper values, the tracker will produce 0% CPU all the
        // time.
        var snapshotsSequence = new[]
        {
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 2).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 3).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 4).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200),
            new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(clock.GetUtcNow().AddMilliseconds(TimerPeriod * 5).Ticks),
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                200)
        };

        // These are the expected CPU% values.
        var expectedCpuValues = new[] { 0, 0, 0, 0, 0 };

        using var autoResetEvent = new AutoResetEvent(false);
        var provider = new FakeProvider();

        // Initially set the provider to return zero snapshot, and use this snapshot
        // to initialize the tracker's internal buffer with zero snapshots.
        provider.SetNextSnapshot(properInitSnapshot);

        var options = new ResourceMonitoringOptions
        {
            CollectionWindow = TimeSpan.FromMilliseconds(TimerPeriod * 2),
            CalculationPeriod = TimeSpan.FromMilliseconds(TimerPeriod * 2),
            SamplingInterval = TimeSpan.FromMilliseconds(TimerPeriod)
        };

        using var tracker = new ResourceUtilizationTrackerService(
            provider,
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(options),
            new List<IResourceUtilizationPublisher>
            {
                new GenericPublisher(_ =>
                {
                    autoResetEvent.Set();
                }),
            },
            clock);

        // Start running the tracker.
        _ = tracker.StartAsync(CancellationToken.None);

        var cpuValuesWithNoHighSpikes = new int[5];

        // In the following cycles the CPU% will be 0% all the time.
        for (int i = 0; i < snapshotsSequence.Length; i++)
        {
            provider.SetNextSnapshot(snapshotsSequence[i]);

            do
            {
                // Advance time.
                clock.Advance(TimeSpan.FromMilliseconds(TimerPeriod));
            }
            while (!autoResetEvent.WaitOne(1));

            var utilization = tracker.GetUtilization(options.CollectionWindow);
            cpuValuesWithNoHighSpikes[i] = (int)utilization.CpuUsedPercentage;
        }

        // Stop the tracker.
        await tracker.StopAsync(CancellationToken.None);

        Assert.Equal(expectedCpuValues, cpuValuesWithNoHighSpikes);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoNotThrow()
    {
        using var tracker = new ResourceUtilizationTrackerService(
            new FakeProvider(),
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(new ResourceMonitoringOptions()),
            new List<IResourceUtilizationPublisher>
            {
                new EmptyPublisher()
            },
            _clock);

        var exception = Record.Exception(() =>
        {
            tracker.Dispose();
            tracker.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task StopAsync_CalledTwice_DoesNotThrow()
    {
        using var tracker = new ResourceUtilizationTrackerService(
            new FakeProvider(),
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(new ResourceMonitoringOptions()),
            new List<IResourceUtilizationPublisher>
            {
                new EmptyPublisher()
            },
            _clock);

        _ = tracker.StartAsync(CancellationToken.None);

        var exception = await Record.ExceptionAsync(async () =>
        {
            await tracker.StopAsync(CancellationToken.None);
            await tracker.StopAsync(CancellationToken.None);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void GetUtilization_BasicTest()
    {
        var providerMock = new Mock<ISnapshotProvider>(MockBehavior.Loose);

        providerMock.Setup(x => x.Resources)
            .Returns(new SystemResources(1.0, 1.0, 100, 100));
        providerMock.Setup(x => x.GetSnapshot())
            .Returns(new ResourceUtilizationSnapshot(
                TimeSpan.FromTicks(_clock.GetUtcNow().Ticks),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                50));

        using var tracker = new ResourceUtilizationTrackerService(
            providerMock.Object,
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromSeconds(1),
                SamplingInterval = TimeSpan.FromMilliseconds(100),
                CalculationPeriod = TimeSpan.FromSeconds(1)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new EmptyPublisher()
            },
            _clock);

        var exception = Record.Exception(() => _ = tracker.GetUtilization(TimeSpan.FromSeconds(1)));

        Assert.Null(exception);
    }

    [Fact]
    public void GetUtilization_ProvidedByWindowGreaterThanSamplingWindowButLesserThanCollectionWindow_Successes()
    {
        var providerMock = new Mock<ISnapshotProvider>(MockBehavior.Loose);

        using var tracker = new ResourceUtilizationTrackerService(
            providerMock.Object,
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromSeconds(6),
                CalculationPeriod = TimeSpan.FromSeconds(5)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new EmptyPublisher(),
            },
            _clock);

        var exception = Record.Exception(() => tracker.GetUtilization(TimeSpan.FromSeconds(6)));

        Assert.Null(exception);
    }

    [Fact]
    public void GetUtilization_ProvidedByWindowGreaterThanBuffer_ThrowsArgumentOutOfRangeException()
    {
        var providerMock = new Mock<ISnapshotProvider>(MockBehavior.Loose);

        using var tracker = new ResourceUtilizationTrackerService(
            providerMock.Object,
            new NullLogger<ResourceUtilizationTrackerService>(),
            Create(new ResourceMonitoringOptions
            {
                CollectionWindow = TimeSpan.FromSeconds(5)
            }),
            new List<IResourceUtilizationPublisher>
            {
                new EmptyPublisher(),
            },
            _clock);

        var exception = Record.Exception(() => tracker.GetUtilization(TimeSpan.FromSeconds(6)));

        Assert.NotNull(exception);
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public async Task Disposing_Service_Twice_Does_Not_Throw()
    {
        using var s = new ResourceUtilizationTrackerService(new FakeProvider(), NullLogger<ResourceUtilizationTrackerService>.Instance,
            Microsoft.Extensions.Options.Options.Create(new ResourceMonitoringOptions()), Array.Empty<IResourceUtilizationPublisher>(), TimeProvider.System);

        var r = await Record.ExceptionAsync(async () =>
        {
            await s.StopAsync(CancellationToken.None);
            await s.StopAsync(CancellationToken.None);
            await s.StopAsync(CancellationToken.None);
        });

        Assert.Null(r);
    }

    private static T? GetDataTrackerField<T>(ResourceUtilizationTrackerService tracker, string name)
    {
        var typ = typeof(ResourceUtilizationTrackerService);
        var type = typ.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        return (T?)type?.GetValue(tracker);
    }
}
