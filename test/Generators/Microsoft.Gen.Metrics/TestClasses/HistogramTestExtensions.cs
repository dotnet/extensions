// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace TestClasses
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "Method body is source generated where the parameters will be used")]
    internal static partial class HistogramTestExtensions
    {
        [Histogram]
        public static partial Histogram0D CreateHistogram0D(Meter meter);

        [Histogram<int>]
        public static partial GenericIntHistogram0D CreateGenericIntHistogram0D(Meter meter);

        [Histogram]
        public static partial HistogramExt0D CreateHistogramExt0D(this Meter meter);

        [Histogram<int>]
        public static partial GenericIntHistogramExt0D CreateGenericIntHistogramExt0D(this Meter meter);

        [Histogram("s1")]
        public static partial Histogram1D CreateHistogram1D(Meter meter);

        [Histogram("s1")]
        public static partial HistogramExt1D CreateHistogramExt1D(this Meter meter);

        [Histogram("s1", "s2")]
        public static partial Histogram2D CreateHistogram2D(Meter meter);

        [Histogram<int>("s1", "s2")]
        public static partial GenericIntHistogram2D CreateGenericIntHistogram2D(Meter meter);

        [Histogram<int>("s1", "s2")]
        public static partial GenericIntHistogramExt2D CreateGenericIntHistogramExt2D(this Meter meter);

        [Histogram("s1", "s2")]
        public static partial HistogramExt2D CreateHistogramExt2D(this Meter meter);

        [Histogram("s1", "s2", "s3")]
        public static partial Histogram3D CreateHistogram3D(Meter meter);

        [Histogram("s1", "s2", "s3")]
        public static partial HistogramExt3D CreateHistogramExt3D(this Meter meter);

        [Histogram("s1", "s2", "s3", "s4")]
        public static partial Histogram4D CreateHistogram4D(Meter meter);

        [Histogram("s1", "s2", "s3", "s4")]
        public static partial HistogramExt4D CreateHistogramExt4D(this Meter meter);

        [Histogram("d1", "d2")]
        public static partial HistogramS0D2 CreateHistogramS0D2(Meter meter);

        [Histogram("d1", "d2")]
        public static partial HistogramExtS0D2 CreateHistogramExtS0D2(this Meter meter);

        [Histogram("s1", "d1")]
        public static partial HistogramS1D1 CreateHistogramS1D1(Meter meter);

        [Histogram("s1", "d1")]
        public static partial HistogramExtS1D1 CreateHistogramExtS1D1(this Meter meter);

        [Histogram("s1", "s2", "s3", "d1", "d2")]
        public static partial HistogramS3D2 CreateHistogramS3D2(Meter meter);

        [Histogram("s1", "s2", "s3", "d1", "d2")]
        public static partial HistogramExtS3D2 CreateHistogramExtS3D2(this Meter meter);

        [Histogram("s1", "s2", "s3", "s4", "s5", "d1", "d2", "d3", "d4", "d5")]
        public static partial HistogramS5D5 CreateHistogramS5D5(Meter meter);

        [Histogram("s1", "s2", "s3", "s4", "s5", "d1", "d2", "d3", "d4", "d5")]
        public static partial HistogramExtS5D5 CreateHistogramExtS5D5(this Meter meter);

        [Histogram("Static:1", "Static-2", "Dyn_1", "Dyn")]
        public static partial TestHistogram CreateTestHistogram(Meter meter);

        [Histogram("Static:1", "Static-2", "Dyn_1", "Dyn")]
        public static partial TestHistogramExt CreateTestHistogramExt(this Meter meter);

        [Histogram(MetricConstants.D1, MetricConstants.D2, MetricConstants.D3, Name = "MyHistogramMetric")]
        public static partial HistogramWithVariableParams CreateHistogramWithVariableParams(Meter meter);

        [Histogram(MetricConstants.D1, MetricConstants.D2, MetricConstants.D3, Name = "MyHistogramMetric")]
        public static partial HistogramExtWithVariableParams CreateHistogramExtWithVariableParams(this Meter meter);

        [Histogram(typeof(HistogramDimensionsTest), Name = "MyHistogramStrongTypeMetric")]
        public static partial StrongTypeHistogram CreateHistogramStrongType(Meter meter);

        [Histogram(typeof(HistogramDimensionsTest), Name = "MyHistogramStrongTypeMetricExt")]
        public static partial StrongTypeHistogramExt CreateHistogramExtStrongType(this Meter meter);

        [Histogram(typeof(HistogramStruct), Name = "MyHistogramStructTypeMetric")]
        public static partial StructTypeHistogram CreateHistogramStructType(Meter meter);

        [Histogram(typeof(HistogramStruct), Name = "MyHistogramStructTypeMetric")]
        public static partial StructTypeHistogramExt CreateHistogramExtStructType(this Meter meter);
    }

    // The order of the below is extremely important for unit testing.
    // The metricGenerator will create the code based on the order it is defined, and the unit test depends on the order being the same.
#pragma warning disable SA1402 // File may only contain a single type
    public class HistogramDimensionsTest : HistogramParentDimensions
    {
        // The generator should ignore these statics:
        public const string Const = "Constant Value";

        public static string Static = "Static Value";

        public string? Dim1;

        public HistogramOperations OperationsEnum { get; set; }

        [TagName("Enum2")]
        public HistogramOperations OperationsEnum2 { get; set; }

        public HistogramChildDimensions? ChildDimensionsObject { get; set; }
        public HistogramGrandChildrenDimensions? GrandChildrenDimensionsObject { get; set; }
    }

    public enum HistogramOperations
    {
        Unknown = 0,
        Operation1 = 1,
    }

    public class HistogramParentDimensions
    {
        public string? ParentOperationName { get; set; }

        public HistogramDimensionsStruct ChildDimensionsStruct { get; set; }
    }

    public class HistogramChildDimensions
    {
        public string? Dim2 { get; set; }

        [TagName("dim2FromAttribute")]
        public string? SomeDim;
    }

    public struct HistogramDimensionsStruct
    {
        public string Dim4Struct { get; set; }

        [TagName("Dim5FromAttribute")]
        public string Dim5Struct { get; set; }
    }

    public class HistogramGrandChildrenDimensions
    {
        public string? Dim3 { get; set; }

        [TagName("Dim3FromAttribute")]
        public string? SomeDim { get; set; }
    }

    public struct HistogramStruct
    {
        // The generator should ignore these statics:
        public const string Const = "Constant Value";

        public static string Static = "Static Value";

        public string? Dim1 { get; set; }

        [TagName("DimInField")]
        public string? DimInField;

        [TagName("Dim2_FromAttribute")]
        public string? Dim2 { get; set; }

        public HistogramOperations Operations { get; set; }

        [TagName("Operations_FromAttribute")]
        public HistogramOperations Operations2 { get; set; }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
