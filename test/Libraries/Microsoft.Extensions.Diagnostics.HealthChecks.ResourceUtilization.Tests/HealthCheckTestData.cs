// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class HealthCheckTestData : IEnumerable<object[]>
{
    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[]
            {
                HealthStatus.Healthy,
                0.1,
                0UL,
                1000UL,
                new ResourceUsageThresholds(),
                new ResourceUsageThresholds(),
                "",
            },
            new object[]
            {
                HealthStatus.Healthy,
                0.2,
                0UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                ""
            },
            new object[]
            {
                HealthStatus.Healthy,
                0.2,
                2UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.2 },
                ""
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.4,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "CPU and memory usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "CPU and memory usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.4, UnhealthyUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.4, UnhealthyUtilizationPercentage = 0.2 },
                "CPU and memory usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.3,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2 },
                "CPU and memory usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { UnhealthyUtilizationPercentage = 0.4 },
                "CPU and memory usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.3,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                "CPU usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Degraded,
                0.1,
                3UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "Memory usage is close to the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.5,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                "CPU usage is above the limit"
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                0.1,
                5UL,
                1000UL,
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.9, UnhealthyUtilizationPercentage = 0.9 },
                new ResourceUsageThresholds { DegradedUtilizationPercentage = 0.2, UnhealthyUtilizationPercentage = 0.4 },
                "Memory usage is above the limit"
            },
        };

    public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
