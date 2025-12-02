// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace TestClasses
{
#pragma warning disable SA1402 // File may only contain a single type
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for classes without namespace")]
    public static partial class MetricsWithUnit
    {
        [Counter(Unit = "seconds")]
        public static partial CounterWithUnit CreateCounterWithUnit(Meter meter);

        [Counter("s1", "s2", Unit = "bytes", Name = "CounterWithUnitAndDims")]
        public static partial CounterWithUnitAndDims CreateCounterWithUnitAndDims(Meter meter);

        [Counter(typeof(Dimensions), Unit = "bytes")]
        public static partial CounterStrongTypeWithUnit CreateCounterStrongTypeWithUnit(Meter meter);

        [Counter<double>(Unit = "meters", Name = "GenericDoubleCounterWithUnit")]
        public static partial GenericDoubleCounterWithUnit CreateGenericDoubleCounterWithUnit(Meter meter);

        [Histogram(Unit = "milliseconds", Name = "HistogramWithUnit")]
        public static partial HistogramWithUnit CreateHistogramWithUnit(Meter meter);

        [Histogram(typeof(Dimensions), Unit = "s")]
        public static partial HistogramStrongTypeWithUnit CreateHistogramStrongTypeWithUnit(Meter meter);

        [Histogram<int>("s1", Unit = "requests", Name = "HistogramWithUnitAndDims")]
        public static partial HistogramWithUnitAndDims CreateHistogramWithUnitAndDims(Meter meter);
    }

    public class Dimensions
    {
        public string? Dim1;
        public string? Dim2;
    }
#pragma warning disable SA1402
}
