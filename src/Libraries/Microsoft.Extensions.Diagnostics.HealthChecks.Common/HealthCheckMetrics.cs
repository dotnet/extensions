﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class HealthCheckMetrics
{
    public HealthCheckMetrics(IMeterFactory meterFactory)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // An issue on analyzer side: https://github.com/dotnet/roslyn-analyzers/issues/6912
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create("Microsoft.Extensions.Diagnostics.HealthChecks");
#pragma warning restore CA2000 // Dispose objects before losing scope

        HealthCheckReportCounter = Metric.CreateHealthCheckReportCounter(meter);
        UnhealthyHealthCheckCounter = Metric.CreateUnhealthyHealthCheckCounter(meter);
    }

    public HealthCheckReportCounter HealthCheckReportCounter { get; }

    public UnhealthyHealthCheckCounter UnhealthyHealthCheckCounter { get; }
}
