// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class NetworkHealthCheckTestData : IEnumerable<object[]>
{
    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[]
            {
                HealthStatus.Healthy,
                10L,
                3L,
                new ResourceCountThresholds(),
                new ResourceCountThresholds(),
                string.Empty,
            },
            new object[]
            {
                HealthStatus.Healthy,
                10L,
                3L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                string.Empty,
            },
            new object[]
            {
                HealthStatus.Healthy,
                100L,
                3L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                string.Empty,
            },
            new object[]
            {
                HealthStatus.Degraded,
                150L,
                3L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "Established connections count is close to the limit",
            },
            new object[]
            {
                HealthStatus.Degraded,
                10L,
                150L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "SYN received connections count is close to the limit",
            },
            new object[]
            {
                HealthStatus.Degraded,
                150L,
                150L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "Network connections count is close to the limit",
            },
            new object[]
            {
                HealthStatus.Degraded,
                10L,
                150L,
                new ResourceCountThresholds(),
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "SYN received connections count is close to the limit",
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                250L,
                3L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "Established connections count is above the limit",
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                10L,
                250L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "SYN received connections count is above the limit",
            },
            new object[]
            {
                HealthStatus.Unhealthy,
                250L,
                250L,
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                new ResourceCountThresholds { DegradedCount = 100, UnhealthyCount = 200 },
                "Network connections count is above the limit",
            },
        };

    public IEnumerator<object[]> GetEnumerator() => Data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
