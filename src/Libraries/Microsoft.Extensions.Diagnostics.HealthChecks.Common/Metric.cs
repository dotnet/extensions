﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.EnumStrings;
using Microsoft.Extensions.Telemetry.Metering;

[assembly: EnumStrings(typeof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus))]

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal static partial class Metric
{
    [Counter("healthy", "status", Name = @"R9\\HealthCheck\\Report")]
    public static partial HealthCheckReportCounter CreateHealthCheckReportCounter(Meter meter);

    [Counter("name", "status", Name = @"R9\\HealthCheck\\UnhealthyHealthCheck")]
    public static partial UnhealthyHealthCheckCounter CreateUnhealthyHealthCheckCounter(Meter meter);

    public static void RecordMetric(this HealthCheckReportCounter counterMetric, bool isHealthy, HealthStatus status)
        => counterMetric.Add(1, isHealthy.ToString(CultureInfo.InvariantCulture), status.ToInvariantString());

    public static void RecordMetric(this UnhealthyHealthCheckCounter counterMetric, string name, HealthStatus status)
        => counterMetric.Add(1, name, status.ToInvariantString());
}
