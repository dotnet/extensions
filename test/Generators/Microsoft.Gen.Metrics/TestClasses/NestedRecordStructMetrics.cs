// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metrics;

namespace NestedRecordStruct.Metrics
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for structs")]
    public static partial class TopLevelStructClass
    {
        public partial record struct InstrumentsInNestedRecordStruct(string Address)
        {
            [Counter<int>]
            public static partial NestedRecordStructCounter CreateCounterInNestedRecordStruct(Meter meter);

            [Histogram<int>]
            public static partial NestedRecordStructHistogram CreateHistogramInNestedRecordStruct(Meter meter);
        }
    }
}
