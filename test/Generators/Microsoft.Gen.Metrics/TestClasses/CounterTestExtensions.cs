// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace TestClasses
{
    internal static partial class CounterTestExtensions
    {
        [Counter<int>]
        public static partial GenericIntCounter0D CreateGenericIntCounter0D(Meter meter);

        [Counter<int>]
        public static partial GenericIntCounterExt0D CreateGenericIntCounterExt0D(this Meter meter);

        [Counter]
        public static partial Counter0D CreateCounter0D(Meter meter);

        [Counter]
        public static partial MyNamedCounter CreateCounterDifferentName(Meter meter);

        [Counter]
        public static partial CounterExt0D CreateCounterExt0D(this Meter meter);

        [Counter<int>("s1")]
        public static partial GenericIntCounter1D CreateGenericIntCounter1D(Meter meter);

        [Counter<int>("s1")]
        public static partial GenericIntCounterExt1D CreateGenericIntCounterExt1D(this Meter meter);

        [Counter<float>("s1")]
        public static partial GenericFloatCounter1D CreateGenericFloatCounter1D(Meter meter);

        [Counter<float>("s1")]
        public static partial GenericFloatCounterExt1D CreateGenericFloatCounterExt1D(this Meter meter);

        [Counter("s1", "s2")]
        public static partial Counter2D CreateCounter2D(Meter meter);

        [Counter("s1", "s2")]
        public static partial CounterExt2D CreateCounterExt2D(this Meter meter);

        [Counter("s1", "s2", "s3")]
        public static partial Counter3D CreateCounter3D(Meter meter);

        [Counter("s1", "s2", "s3")]
        public static partial CounterExt3D CreateCounterExt3D(this Meter meter);

        [Counter("s1", "s2", "s3", "s4")]
        public static partial Counter4D CreateCounter4D(Meter meter);

        [Counter("s1", "s2", "s3", "s4")]
        public static partial CounterExt4D CreateCounterExt4D(this Meter meter);

        [Counter("d1", "d2")]
        public static partial CounterS0D2 CreateCounterS0D2(Meter meter);

        [Counter("d1", "d2")]
        public static partial CounterExtS0D2 CreateCounterExtS0D2(this Meter meter);

        [Counter("s1", "d1")]
        public static partial CounterS1D1 CreateCounterS1D1(Meter meter);

        [Counter("s1", "d1")]
        public static partial CounterExtS1D1 CreateCounterExtS1D1(this Meter meter);

        [Counter("s1", "s2", "s3", "d1", "d2")]
        public static partial CounterS3D2 CreateCounterS3D2(Meter meter);

        [Counter("s1", "s2", "s3", "d1", "d2")]
        public static partial CounterExtS3D2 CreateCounterExtS3D2(this Meter meter);

        [Counter("s1", "s2", "s3", "s4", "s5", "d1", "d2", "d3", "d4", "d5")]
        public static partial CounterS5D5 CreateCounterS5D5(Meter meter);

        [Counter("s1", "s2", "s3", "s4", "s5", "d1", "d2", "d3", "d4", "d5")]
        public static partial CounterExtS5D5 CreateCounterExtS5D5(this Meter meter);

        [Counter("Static:1", "Static-2", "Dyn_1", "Dyn")]
        public static partial TestCounter CreateTestCounter(Meter meter);

        [Counter("Static:1", "Static-2", "Dyn_1", "Dyn")]
        public static partial TestCounterExt CreateTestCounterExt(this Meter meter);

        [Counter(MetricConstants.D1, MetricConstants.D2, MetricConstants.D3, Name = @"MyCounterMetric")]
        public static partial CounterWithVariableParams CreateCounterWithVariableParams(Meter meter);

        [Counter(MetricConstants.D1, MetricConstants.D2, MetricConstants.D3, Name = @"MyCounterMetric")]
        public static partial CounterExtWithVariableParams CreateCounterExtWithVariableParams(this Meter meter);

        [Counter(Name = @"MyMetric\Category\SingleSlash")]
        public static partial CounterX CreateCounterX(Meter meter);

        [Counter(Name = @"MyMetric\Category\SingleSlash")]
        public static partial CounterExtX CreateCounterExtX(this Meter meter);

        [Counter(Name = @"MyMetric\\Category\\DoubleSlash")]
        public static partial CounterY CreateCounterY(Meter meter);

        [Counter(Name = @"MyMetric\\Category\\DoubleSlash")]
        public static partial CounterExtY CreateCounterExtY(this Meter meter);

        [Counter<decimal>(typeof(CounterDimensions), Name = "MyCounterStrongTypeMetric")]
        public static partial StrongTypeDecimalCounter CreateStrongTypeDecimalCounter(Meter meter);

        [Counter<decimal>(typeof(CounterDimensions), Name = "MyCounterStrongTypeMetricExt")]
        public static partial StrongTypeDecimalCounterExt CreateStrongTypeDecimalCounterExt(this Meter meter);

        [Counter<long>(typeof(CounterStructDimensions), Name = "MyCounterStructTypeMetric")]
        public static partial StructTypeCounter CreateCounterStructType(Meter meter);

        [Counter<long>(typeof(CounterStructDimensions), Name = "MyCounterStructTypeMetric")]
        public static partial StructTypeCounterExt CreateCounterStructTypeExt(this Meter meter);

        [Counter<long>(typeof(CounterRecordClassDimensions), Name = "MyCounterRecordClassTypeMetric")]
        public static partial RecordClassTypeCounter CreateCounterRecordClassType(Meter meter);
    }
}
