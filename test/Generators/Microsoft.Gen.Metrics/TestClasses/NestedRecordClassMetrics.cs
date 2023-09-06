// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metrics;

namespace NestedRecordClass.Metrics
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for records")]
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
