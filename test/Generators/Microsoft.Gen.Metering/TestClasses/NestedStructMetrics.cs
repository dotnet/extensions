// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace NestedStruct.Metering
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for classes")]
    [SuppressMessage("Readability", "R9A046:Source generated metrics (fast metrics) should be located in 'Metric' class",
        Justification = "Metering generator tests")]
    public partial struct TopLevelStruct
    {
        internal partial struct InstrumentsInNestedStruct
        {
            [Counter<int>]
            public static partial NestedStructCounter CreateCounterInNestedStruct(Meter meter);

            [Histogram<int>]
            public static partial NestedStructHistogram CreateHistogramInNestedStruct(Meter meter);
        }
    }
}
