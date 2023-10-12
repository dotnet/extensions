// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

/// <summary>
/// Tests for the DataTracker utilization calculator.
/// </summary>
public sealed class CalculatorTests
{
    private const double CpuUnits = 1;
    private const ulong TotalMemoryInBytes = 1000;

    private readonly Snapshot _firstSnapshot = new(
        totalTimeSinceStart: TimeSpan.FromTicks(new FakeTimeProvider().GetUtcNow().Ticks),
        kernelTimeSinceStart: TimeSpan.FromTicks(0),
        userTimeSinceStart: TimeSpan.FromTicks(0),
        memoryUsageInBytes: 0);
    private readonly SystemResources _resources = new(CpuUnits, CpuUnits, TotalMemoryInBytes, TotalMemoryInBytes);

    /// <summary>
    /// Ensure that CPU stats work appropriately.
    /// </summary>
    [Fact]
    public void BasicCalculation()
    {
        var secondSnapshotTimeSpan = _firstSnapshot.TotalTimeSinceStart.Add(TimeSpan.FromSeconds(5));

        // Now, what's the total number of available ticks between the two samples (for a single core)
        var totalAvailableTicks = secondSnapshotTimeSpan.Ticks - _firstSnapshot.TotalTimeSinceStart.Ticks;

        var second = new Snapshot(
            totalTimeSinceStart: secondSnapshotTimeSpan,

            // assign 25% to kernel time
            kernelTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks / 4),

            // assign 25% to user time
            userTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks / 4),
            memoryUsageInBytes: 500);

        // Now, when we run the calculator, CPU should be at 50%.
        var record = Calculator.CalculateUtilization(_firstSnapshot, second, _resources);
        Assert.Equal(50.0, record.CpuUsedPercentage);

        // Because we set it basically, memory should also clearly be at 50%.
        Assert.Equal(50.0, record.MemoryUsedPercentage);

        // Memory totals should reflect the second sample
        Assert.Equal(500UL, record.MemoryUsedInBytes);
        Assert.Equal(1000UL, record.SystemResources.GuaranteedMemoryInBytes);
    }

    /// <summary>
    /// Ensure that CPU stats work appropriately.
    /// </summary>
    [Fact]
    public void BasicCalculation_WithHalfCpuUnits()
    {
        var limitedResources = new SystemResources(0.5, 0.5, TotalMemoryInBytes, TotalMemoryInBytes);

        var secondSnapshotTimeSpan = _firstSnapshot.TotalTimeSinceStart.Add(TimeSpan.FromSeconds(5));

        // Now, what's the total number of available ticks between the two samples (for a single core)
        var totalAvailableTicks = secondSnapshotTimeSpan.Ticks - _firstSnapshot.TotalTimeSinceStart.Ticks;

        var second = new Snapshot(
            totalTimeSinceStart: secondSnapshotTimeSpan,

            // assign 25% to kernel time
            kernelTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks / 4),

            // assign 25% to user time
            userTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks / 4),
            memoryUsageInBytes: 500);

        // Using the limited resources, CPU time is now cut in half. So, when we run
        // the calculator, the CPU utilization should be at 100%.
        var record = Calculator.CalculateUtilization(_firstSnapshot, second, limitedResources);
        Assert.Equal(100.0, record.CpuUsedPercentage);
    }

    /// <summary>
    /// Ensure that stats work appropriately at zero percent.
    /// </summary>
    [Fact]
    public void Zeroes()
    {
        // No changes in the second snapshot
        var secondSnapshot = new Snapshot(
            totalTimeSinceStart: _firstSnapshot.TotalTimeSinceStart.Add(TimeSpan.FromSeconds(5)),
            memoryUsageInBytes: 0,
            kernelTimeSinceStart: _firstSnapshot.KernelTimeSinceStart,
            userTimeSinceStart: _firstSnapshot.UserTimeSinceStart);

        // Now, let's set each of kernel and user time to no time elapsed.

        // Now, when we run the calculator, CPU should be at 0%.
        var record = Calculator.CalculateUtilization(_firstSnapshot, secondSnapshot, _resources);
        Assert.Equal(0.0, record.CpuUsedPercentage);

        // Because we set it basically, memory should also clearly be at 0%.
        Assert.Equal(0.0, record.MemoryUsedPercentage);

        // Memory totals should reflect the second sample
        Assert.Equal(0UL, record.MemoryUsedInBytes);
        Assert.Equal(1000UL, record.SystemResources.GuaranteedMemoryInBytes);
    }

    /// <summary>
    /// Ensure that stats work appropriately if time goes backwards.
    /// </summary>
    /// <remarks>The lowest possible CPU percentage should be zero.</remarks>
    [Fact]
    public void TimeGoesBackwards()
    {
        var firstSnapshot = new Snapshot(
        totalTimeSinceStart: TimeSpan.FromTicks(new FakeTimeProvider().GetUtcNow().Ticks),
        kernelTimeSinceStart: TimeSpan.FromTicks(1000),
        userTimeSinceStart: TimeSpan.FromTicks(1000),
        memoryUsageInBytes: 0);
        var secondSnapshot = new Snapshot(
            totalTimeSinceStart: firstSnapshot.TotalTimeSinceStart.Add(TimeSpan.FromSeconds(5)),
            memoryUsageInBytes: 0,
            kernelTimeSinceStart: TimeSpan.FromTicks(firstSnapshot.KernelTimeSinceStart.Ticks - 1),
            userTimeSinceStart: TimeSpan.FromTicks(firstSnapshot.UserTimeSinceStart.Ticks - 1));

        // Now, when we run the calculator, CPU should be at 0%.
        var record = Calculator.CalculateUtilization(firstSnapshot, secondSnapshot, _resources);
        Assert.Equal(0.0, record.CpuUsedPercentage);
    }

    /// <summary>
    /// Ensure that stats work appropriately if we seem to spend all the CPU time.
    /// </summary>
    /// <remarks>The highest possible CPU percentage should be 100%.</remarks>
    [Fact]
    public void FullyUtilized()
    {
        var secondSnapshotTimeSpan = _firstSnapshot.TotalTimeSinceStart.Add(TimeSpan.FromSeconds(5));

        // Now, what's the total number of available ticks between the two samples.
        var totalAvailableTicks = secondSnapshotTimeSpan.Ticks - _firstSnapshot.TotalTimeSinceStart.Ticks;

        var secondSnapshot = new Snapshot(
            totalTimeSinceStart: secondSnapshotTimeSpan,
            kernelTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks / 2),
            userTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks / 2),
            memoryUsageInBytes: 1000);

        // Now, when we run the calculator, CPU should 100%.
        var record = Calculator.CalculateUtilization(_firstSnapshot, secondSnapshot, _resources);
        Assert.Equal(100.0, record.CpuUsedPercentage);

        // Assert that memory is at 100%.
        Assert.Equal(100.0, record.MemoryUsedPercentage);
        Assert.Equal(1000UL, record.MemoryUsedInBytes);
        Assert.Equal(1000UL, record.SystemResources.GuaranteedMemoryInBytes);
    }

    /// <summary>
    /// Ensure that stats work appropriately if the resources are overutilized.
    /// </summary>
    /// <remarks>The highest possible CPU and memory percentages should be 100%.</remarks>
    [Fact]
    public void OverUtilized()
    {
        var secondSnapshotTimeSpan = _firstSnapshot.TotalTimeSinceStart.Add(TimeSpan.FromSeconds(5));

        // Now, what's the total number of available ticks between the two samples.
        var totalAvailableTicks = secondSnapshotTimeSpan.Ticks - _firstSnapshot.TotalTimeSinceStart.Ticks;

        var secondSnapshot = new Snapshot(
            totalTimeSinceStart: secondSnapshotTimeSpan,

            // Set each of kernel and uesr time to all the available ticks between
            // the 2 snapshots to make them sum to 200% of the total CPU time.
            kernelTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks),
            userTimeSinceStart: TimeSpan.FromTicks(totalAvailableTicks),
            memoryUsageInBytes: 1500);

        // Now, when we run the calculator, CPU should be at 100%.
        var record = Calculator.CalculateUtilization(_firstSnapshot, secondSnapshot, _resources);
        Assert.Equal(100.0, record.CpuUsedPercentage);

        // Assert that memory is at 100%
        Assert.Equal(100.0, record.MemoryUsedPercentage);
        Assert.Equal(1500UL, record.MemoryUsedInBytes);
        Assert.Equal(1000UL, record.SystemResources.GuaranteedMemoryInBytes);
    }
}
