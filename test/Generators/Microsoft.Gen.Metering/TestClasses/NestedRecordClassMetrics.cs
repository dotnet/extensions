// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace NestedRecordClass.Metering
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for records")]
    [SuppressMessage("Readability", "R9A046:Source generated metrics (fast metrics) should be located in 'Metric' class",
        Justification = "Metering generator tests")]
    public static partial class TopLevelRecordClass
    {
        public partial record class InstrumentsInNestedRecordClass(string Address)
        {
            [Counter<int>]
            public static partial NestedRecordClassCounter CreateCounterInNestedRecordClass(Meter meter);

            [Histogram<int>]
            public static partial NestedRecordClassHistogram CreateHistogramInNestedRecordClass(Meter meter);
        }
    }
}
