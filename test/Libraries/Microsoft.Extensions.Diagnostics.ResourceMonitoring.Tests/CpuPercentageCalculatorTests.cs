// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

public class CpuPercentageCalculatorTests
{
    private const double TicksPerSecondDouble = TimeSpan.TicksPerSecond; // 10,000,000
    private const int MetricValueMultiplier = 100;

    public class TestCpuCalculator
    {
        private readonly double _cpuLimit;
        private ulong _oldCpuUsageTicks;
        private long _oldCpuTimeTicks;

        public TestCpuCalculator(double cpuLimit)
        {
            _cpuLimit = cpuLimit;
        }

        public void SetInitialState(ulong initialCpuTicks, long initialTimeTicks)
        {
            _oldCpuUsageTicks = initialCpuTicks;
            _oldCpuTimeTicks = initialTimeTicks;
        }

        public double CalculateCpuPercentage(ulong currentCpuTicks, long currentTimeTicks)
        {
            var usageTickDelta = currentCpuTicks - _oldCpuUsageTicks;
            var timeTickDelta = (currentTimeTicks - _oldCpuTimeTicks) * _cpuLimit;

            if (usageTickDelta > 0 && timeTickDelta > 0)
            {
                return Math.Min(MetricValueMultiplier, usageTickDelta / timeTickDelta * MetricValueMultiplier);
            }
            return 0;
        }
    }

    [Theory]
    [InlineData(0.3, 4, 1.2, "Current implementation - with core scaling")]
    [InlineData(0.3, 1, 0.3, "Fixed implementation - without core scaling")]
    public void CpuPercentageCalculation_WithDifferentLimits_ShowsScalingImpact(
        double kubernetesLimit, int coreCount, double expectedCpuLimit, string testCase)
    {
        // Arrange - Simulate GetCpuLimit calculation
        double cpuLimit = kubernetesLimit * coreCount; // Your current implementation
        var calculator = new TestCpuCalculator(cpuLimit);

        // Initial state - container has been running and consumed some CPU
        var initialTime = new DateTime(2025, 1, 1, 12, 0, 0).Ticks;
        var initialCpuTicks = (ulong)(1.5 * TicksPerSecondDouble); // 1.5 seconds total CPU consumed
        calculator.SetInitialState(initialCpuTicks, initialTime);

        // After 5 seconds, container consumed 0.2 more CPU seconds
        var currentTime = initialTime + (5 * TimeSpan.TicksPerSecond); // 5 seconds later  
        var currentCpuTicks = initialCpuTicks + (ulong)(0.2 * TicksPerSecondDouble); // +0.2 CPU seconds

        // Act
        var cpuPercentage = calculator.CalculateCpuPercentage(currentCpuTicks, currentTime);

        // Assert & Debug
        var actualCpuUsagePercent = (0.2 / 5.0) * 100; // 4% of wall clock time
        var expectedPercentOfLimit = (0.2 / kubernetesLimit) / 5.0 * 100; // % of allocated CPU budget

        Console.WriteLine($"\n=== {testCase} ===");
        Console.WriteLine($"Kubernetes CPU limit: {kubernetesLimit} cores");
        Console.WriteLine($"System cores: {coreCount}");
        Console.WriteLine($"Calculated _cpuLimit: {cpuLimit}");
        Console.WriteLine($"Actual CPU usage: 0.2 cores over 5 seconds = {actualCpuUsagePercent:F1}% of wall clock");
        Console.WriteLine($"Expected % of CPU budget: {expectedPercentOfLimit:F1}%");
        Console.WriteLine($"Your algorithm reports: {cpuPercentage:F1}%");
        Console.WriteLine($"Difference factor: {expectedPercentOfLimit / cpuPercentage:F1}x");

        // Verify the scaling issue
        if (testCase.Contains("Current implementation"))
        {
            Assert.True(cpuPercentage < 5, $"Current implementation should show low percentage due to scaling issue. Got: {cpuPercentage}%");
        }
        else
        {
            Assert.InRange(cpuPercentage, 10, 15); // Should be around 13.3% with correct scaling
        }
    }

    [Fact]
    public void CpuPercentage_RealisticKubernetesScenario_ShowsProblem()
    {
        // Arrange - Your exact scenario
        // K8s: 0.3 CPU limit, 4-core node
        double cpuLimit = 0.3 * 4; // 1.2 (current implementation)
        var calculator = new TestCpuCalculator(cpuLimit);

        // Container running for a while, consumed 2 seconds of CPU total
        var startTime = DateTime.UtcNow.AddMinutes(-5).Ticks;
        calculator.SetInitialState((ulong)(2.0 * TicksPerSecondDouble), startTime);

        // 5 seconds later, consumed 0.15 more CPU seconds (moderate load)
        var currentTime = startTime + (5 * TimeSpan.TicksPerSecond);
        var currentCpuTicks = (ulong)((2.0 + 0.15) * TicksPerSecondDouble);

        // Act
        var result = calculator.CalculateCpuPercentage(currentCpuTicks, currentTime);

        // Assert
        Console.WriteLine($"\nRealistic Scenario:");
        Console.WriteLine($"Used 0.15 CPU cores over 5 seconds");
        Console.WriteLine($"That's {(0.15 / 5.0) * 100:F1}% of wall clock time");
        Console.WriteLine($"With 0.3 CPU limit, that's {(0.15 / 0.3 / 5.0) * 100:F1}% of CPU budget per second");
        Console.WriteLine($"Your algorithm reports: {result:F1}%");

        // This demonstrates the problem - should be around 10%, but will be much lower
        Assert.True(result < 5, "Should show deflated percentage due to core scaling");

        // Shows what it should be without scaling
        var correctLimit = 0.3;
        var correctTimeTickDelta = (currentTime - startTime) * correctLimit;
        var correctPercentage = ((currentCpuTicks - (ulong)(2.0 * TicksPerSecondDouble)) / correctTimeTickDelta) * 100;
        Console.WriteLine($"With correct scaling (0.3 limit): {correctPercentage:F1}%");
    }
}
