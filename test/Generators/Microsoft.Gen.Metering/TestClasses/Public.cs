// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace PublicMetering
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for classes without namespace")]
    [SuppressMessage("Readability", "R9A046:Source generated metrics (fast metrics) should be located in 'Metric' class",
        Justification = "Metering generator tests")]
    public static partial class PublicMetricInstruments
    {
        [Counter]
        public static partial PublicCounter CreatePublicCounter(Meter meter);

        [Histogram]
        public static partial PublicHistogram CreatePublicHistogram(Meter meter);
    }
}
