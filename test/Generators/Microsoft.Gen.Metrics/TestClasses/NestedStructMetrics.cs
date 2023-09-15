// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace NestedStruct.Metrics
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for classes")]
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
