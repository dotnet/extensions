// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.EnumStrings;

[assembly: EnumStrings(typeof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus))]

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal static partial class Metric
{
    [Counter("dotnet.health_check.status", Name = "dotnet.health_check.reports")]
    public static partial HealthCheckReportCounter CreateHealthCheckReportCounter(Meter meter);

    [Counter("dotnet.health_check.name", "dotnet.health_check.status", Name = "dotnet.health_check.unhealthy_checks")]
    public static partial UnhealthyHealthCheckCounter CreateUnhealthyHealthCheckCounter(Meter meter);

    public static void RecordMetric(this HealthCheckReportCounter counterMetric, HealthStatus status)
        => counterMetric.Add(1, status.ToInvariantString());

    public static void RecordMetric(this UnhealthyHealthCheckCounter counterMetric, string name, HealthStatus status)
        => counterMetric.Add(1, name, status.ToInvariantString());
}
