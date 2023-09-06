// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Metrics;

namespace TestClasses
{
    internal static partial class MeterTExtensions
    {
        internal sealed class DummyType
        {
        }

        [Counter<int>]
        public static partial GenericIntCounter0DMeterT CreateGenericIntCounter0D(Meter<DummyType> meter);

        [Counter<int>]
        public static partial GenericIntCounterExt0DMeterT CreateGenericIntCounterExt0D(this Meter<DummyType> meter);

        [Counter]
        public static partial Counter0DMeterT CreateCounter0D(Meter<DummyType> meter);

        [Counter]
        public static partial MyNamedCounterMeterT CreateCounterDifferentName(Meter<DummyType> meter);

        [Counter]
        public static partial CounterExt0DMeterT CreateCounterExt0D(this Meter<DummyType> meter);

        [Counter("s1", "s2", "s3", "s4", "s5", "d1", "d2", "d3", "d4", "d5")]
        public static partial CounterS5D5MeterT CreateCounterS5D5(Meter<DummyType> meter);

        [Counter("s1", "s2", "s3", "s4", "s5", "d1", "d2", "d3", "d4", "d5")]
        public static partial CounterExtS5D5MeterT CreateCounterExtS5D5(this Meter<DummyType> meter);

        [Counter("Static:1", "Static-2", "Dyn_1", "Dyn")]
        public static partial TestCounterMeterT CreateTestCounter(Meter<DummyType> meter);

        [Counter("Static:1", "Static-2", "Dyn_1", "Dyn")]
        public static partial TestCounterExtMeterT CreateTestCounterExt(this Meter<DummyType> meter);

        [Counter(MetricConstants.D1, MetricConstants.D2, MetricConstants.D3, Name = @"MyCounterMetric")]
        public static partial CounterWithVariableParamsMeterT CreateCounterWithVariableParams(Meter<DummyType> meter);

        [Counter(MetricConstants.D1, MetricConstants.D2, MetricConstants.D3, Name = @"MyCounterMetric")]
        public static partial CounterExtWithVariableParamsMeterT CreateCounterExtWithVariableParams(this Meter<DummyType> meter);

        [Counter<decimal>(typeof(CounterDimensions), Name = "MyCounterStrongTypeMetric")]
        public static partial StrongTypeDecimalCounterMeterT CreateStrongTypeDecimalCounter(Meter<DummyType> meter);

        [Counter<decimal>(typeof(CounterDimensions), Name = "MyCounterStrongTypeMetric")]
        public static partial StrongTypeDecimalCounterExtMeterT CreateStrongTypeDecimalCounterExt(this Meter<DummyType> meter);

        [Counter<long>(typeof(CounterStructDimensions), Name = "MyCounterStructTypeMetric")]
        public static partial StructTypeCounterMeterT CreateCounterStructType(Meter<DummyType> meter);

        [Counter<long>(typeof(CounterStructDimensions), Name = "MyCounterStructTypeMetric")]
        public static partial StructTypeCounterExtMeterT CreateCounterStructTypeExt(this Meter<DummyType> meter);
    }
}
