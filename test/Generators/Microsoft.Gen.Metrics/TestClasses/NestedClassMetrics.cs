// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace NestedClass.Metrics
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for classes")]
    public static partial class TopLevelClass
    {
        internal static partial class InstrumentsInNestedClass
        {
            [Counter<int>]
            public static partial NestedClassCounter CreateCounterInNestedClass(Meter meter);

            [Histogram<int>]
            public static partial NestedClassHistogram CreateHistogramInNestedClass(Meter meter);
        }

        internal static partial class MultiLevelNesting
        {
            internal static partial class MultiLevelNestedClass
            {
                [Counter<int>]
                public static partial MultiLevelNestedClassCounter CreateCounterInMultiLevelNestedClass(Meter meter);

                [Histogram<int>]
                public static partial MultiLevelNestedClassHistogram CreateHistogramInMultiLevelNestedClass(Meter meter);
            }
        }
    }
}
