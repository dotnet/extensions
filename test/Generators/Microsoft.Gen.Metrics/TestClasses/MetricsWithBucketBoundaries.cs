// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET10_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace TestClasses
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "Method body is source generated where the parameters will be used")]
    public static partial class MetricsWithBucketBoundaries
    {
        [Histogram<double>("s1", Unit = "s", Name = "DoubleHistogramWithBuckets",
            HistogramBucketBoundaries = [double.NegativeInfinity, 1E-05, 0.005, 0.1, 1, 10, double.PositiveInfinity])]
        public static partial DoubleHistogramWithBuckets CreateDoubleHistogramWithBuckets(Meter meter);

        [Histogram(Description = "Long histogram with buckets.", HistogramBucketBoundaries = [long.MinValue, 0, 9_000_000_000])]
        public static partial LongHistogramWithBuckets CreateLongHistogramWithBuckets(Meter meter);

        [Histogram<float>(typeof(Dimensions), HistogramBucketBoundaries = [0.1f, 0.25f, 2.5f])]
        public static partial FloatHistogramStrongTypeWithBuckets CreateFloatHistogramStrongTypeWithBuckets(Meter meter);

        [Histogram<int>(HistogramBucketBoundaries = [])]
        public static partial IntHistogramWithoutBuckets CreateIntHistogramWithoutBuckets(Meter meter);
    }
}
#endif
