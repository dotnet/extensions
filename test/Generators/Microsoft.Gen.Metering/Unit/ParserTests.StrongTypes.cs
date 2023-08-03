// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Gen.Metering.Test;

public partial class ParserTests
{
    [Fact]
    public async Task NullDimensionNamesInAttributes()
    {
        var d = await RunGenerator(@"
            public struct HistogramStruct
            {
                [Dimension(null)]
                public string? Dim1 { get; set; }
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(HistogramStruct), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StructTypeHistogram()
    {
        var d = await RunGenerator(@"
            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public struct HistogramStruct
            {
                [Dimension(""Dim1_FromAttribute"")]
                public string? Dim1 { get; set; }

                [Dimension(""Operations_FromAttribute"")]
                public Operations Operations { get; set; }
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(HistogramStruct), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StrongTypeHistogram()
    {
        // This test should return no errors.
        var d = await RunGenerator(@"
            public class DimensionsTest : ParentDimensions
            {
                public string? test1 { get; set; }

                [Dimension(""test1_FromAttribute"")]
                public string? test1_WithAttribute { get; set; }

                [Dimension(""operations_FromAttribute"")]
                public Operations operations {get;set;}

                public ChildDimensions? ChildDimensions1 { get; set; }

                public void Method()
                {
                    System.Console.WriteLine(""I am a method."");
                }
            }

            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public class ParentDimensions
            {
                [Dimension(""parentDimension_FromAttribute"")]
                public string? ParentOperationNameWithAttribute { get;set; }

                public string? ParentOperationName { get;set; }

                public DimensionsStruct ChildDimensionsStruct { get; set; }
            }

            public class ChildDimensions
            {
                [Dimension(""test2_FromAttribute"")]
                public string test2_WithAttribute { get; set; }

                public string test2 { get; set; }

                [Dimension(""test1_FromAttribute_In_Child1"")]
                public string? test1 { get; set; }

                public ChildDimensions2? ChildDimensions2 { get; set;}
            }

            public class ChildDimensions2
            {
                [Dimension(""test3_FromAttribute"")]
                public string test3_WithAttribute { get; set; }

                public string test3 { get; set; }

                [Dimension(""test1_FromAttribute_In_Child2"")]
                public string? test1 { get; set; }
            }

            public struct DimensionsStruct
            {
                [Dimension(""testStruct_FromAttribute"")]
                public string testStruct_WithAttribute { get; set; }

                public string testStruct { get; set; }
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task SimpleStrongTypeHistogram()
    {
        // This test should return no errors.
        var d = await RunGenerator(@"
            public struct DimensionsTest
            {
                [Dimension(""test1_FromAttribute"")]
                public string? test1_WithAttribute { get; set; }
                public string? test1 { get; set; }
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest))]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task TestNoStrongTypeDefined()
    {
        var d = await RunGenerator(@"
            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest))]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StructTypeCounter()
    {
        var d = await RunGenerator(@"
            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public struct CounterStruct
            {
                [Dimension(""Dim1_FromAttribute"")]
                public string? Dim1 { get; set; }

                [Dimension(""Dim2_FromAttribute"")]
                public string? Dim2;

                [Dimension(""Operations_FromAttribute"")]
                public Operations Operations { get; set; }
            }

            public static partial class MetricClass
            {
                [Counter(typeof(CounterStruct), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }
        ");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StrongTypeCounter()
    {
        // This test should return no errors.
        var d = await RunGenerator(@"
            public class DimensionsTest : ParentDimensions
            {
                [Dimension(""test1_FromAttribute"")]
                public string? test1 { get; set; }

                [Dimension(""Operations_FromAttribute"")]
                public Operations operations {get;set;}

                public ChildDimensions? ChildDimensions1 { get; set; }

                public void Method()
                {
                    System.Console.WriteLine(""I am a method."");
                }
            }

            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public class ParentDimensions
            {
                [Dimension(""parentDimension_FromAttribute"")]
                public string? ParentOperationNameWithAttribute { get;set; }

                public string? ParentOperationName { get;set; }

                public DimensionsStruct ChildDimensionsStruct { get; set; }
            }

            public class ChildDimensions
            {
                [Dimension(""test2_FromAttribute"")]
                public string test2_WithAttribute { get; set; }

                public string test2 { get; set; }

                [Dimension(""test1_FromAttribute_In_Child1"")]
                public string? test1 { get; set; }
            }

            public struct DimensionsStruct
            {
                [Dimension(""testStruct_FromAttribute"")]
                public string testStruct_WithAttribute { get; set; }

                public string testStruct { get; set; }
            }

            public static partial class MetricClass
            {
                [Counter(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StrongTypeCounterWithDescription()
    {
        var d = await RunGenerator(@"
        internal static partial class Metric
        {
            /// <summary>
            /// Dimension1 description.
            /// </summary>
            public const string Dim1 = ""Dim1"";

            /// <summary>
            /// DescribedDimensionCounter description.
            /// </summary>
            /// <param name=""meter""></param>
            /// <returns></returns>
            [Counter(DescripedDimensions.Dimension1, Dim1)]
            public static partial DescribedDimensionCounter CreatePublicCounter(Meter meter);

            /// <summary>
            /// DimenisonDefinedInMetricClass description.
            /// </summary>
            public const string DimenisonDefinedInMetricClass = ""DimenisonDefinedInMetricClass"";

            /// <summary>
            /// DescribedDimensionHistogram description.
            /// </summary>
            /// <param name=""meter""></param>
            /// <returns></returns>
            [Histogram(DescripedDimensions.Dimension2, DimenisonDefinedInMetricClass)]
            public static partial DescribedDimensionHistogram CreatePublicHistogram(Meter meter);

            /// <summary>
            /// StrongTypeCounterWithDescripedDimension description.
            /// </summary>
            /// <param name=""meter""></param>
            /// <returns></returns>
            [Counter(typeof(DimensionForStrongTypes), Name = ""MyStrongTypeMetricWithDescription"")]
            public static partial StrongTypeCounterWithDescripedDimension CreateStrongTypeCounterWithDescibedDimensions(Meter meter);
        }

        /// <summary>
        /// DescripedDimensions class description.
        /// </summary>
        internal static class DescripedDimensions
        {
            /// <summary>
            /// Dimension1 in class description.
            /// </summary>
            public const string Dimension1 = ""Dimension1"";

            /// <summary>
            /// Dimension2 description.
            /// </summary>
            public const string Dimension2 = ""Dimension2"";

            /// <summary>
            /// Dimension3 description.
            /// </summary>
            public const string Dimension3 = ""Dimension3"";
        }

        public class DimensionForStrongTypes
        {
            /// <summary>
            /// Gets or sets anotherDimension.
            /// </summary>
            public string? AnotherDimension { get; set; }

            /// <summary>
            /// Gets or sets MetricEnum.
            /// </summary>
            public MetricOperations MetricEnum { get; set; }

            /// <summary>
            /// Gets or sets MetricEnum2.
            /// </summary>
            [Dimension(""Enum2"")]
            public MetricOperations MetricEnum2 { get; set; }

            /// <summary>
            /// Gets or sets ChildDimensionsClass.
            /// </summary>
            public ChildClassDimensionForStrongTypes? ChildDimensionsClass { get; set; }

            /// <summary>
            ///  Gets or sets ChildDimensionsStruct.
            /// </summary>
            public DimensionForStrongTypesDimensionsStruct ChildDimensionsStruct { get; set; }
        }

        public enum MetricOperations
        {
            Unknown = 0,
            Operation1 = 1,
        }

        public class ChildClassDimensionForStrongTypes
        {
            /// <summary>
            /// Gets or sets Dim2.
            /// </summary>
            public string? Dim2 { get; set; }

            /// <summary>
            /// Gets or sets SomeDim.
            /// </summary>
            [Dimension(""dim2FromAttribute"")]
            public string? SomeDim;
        }

        public struct DimensionForStrongTypesDimensionsStruct
        {
            /// <summary>
            /// Gets or sets Dim4Struct.
            /// </summary>
            public string Dim4Struct { get; set; }

            /// <summary>
            /// Gets or sets Dim5Struct.
            /// </summary>
            [Dimension(""Dim5FromAttribute"")]
            public string Dim5Struct { get; set; }
        }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StrongTypeHistogramWithDescription()
    {
        // This test should return no errors.
        var d = await RunGenerator(@"
            public class DimensionsTest : ParentDimensions
            {
                /// <summary>
                /// test1 description.
                /// </summary>
                public string? test1 { get; set; }

                /// <summary>
                /// test1_FromAttribute description.
                /// </summary>
                [Dimension(""test1_FromAttribute"")]
                public string? test1_WithAttribute { get; set; }

                /// <summary>
                /// operations_FromAttribute description.
                /// </summary>
                [Dimension(""operations_FromAttribute"")]
                public Operations operations {get;set;}

                /// <summary>
                /// ChildDimensions1 description.
                /// </summary>
                public ChildDimensions? ChildDimensions1 { get; set; }

                public void Method()
                {
                    System.Console.WriteLine(""I am a method."");
                }
            }

            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public class ParentDimensions
            {
                /// <summary>
                /// parentDimension_FromAttribute description.
                /// </summary>
                [Dimension(""parentDimension_FromAttribute"")]
                public string? ParentOperationNameWithAttribute { get;set; }

                /// <summary>
                /// ParentOperationName description.
                /// </summary>
                public string? ParentOperationName { get;set; }

                public DimensionsStruct ChildDimensionsStruct { get; set; }
            }

            public class ChildDimensions
            {
                /// <summary>
                /// test2_WithAttribute description.
                /// </summary>
                [Dimension(""test2_FromAttribute"")]
                public string test2_WithAttribute { get; set; }

                /// <summary>
                /// test2 description.
                /// </summary>
                public string test2 { get; set; }

                /// <summary>
                /// test1_FromAttribute_In_Child1 description.
                /// </summary>
                [Dimension(""test1_FromAttribute_In_Child1"")]
                public string? test1 { get; set; }

                public ChildDimensions2? ChildDimensions2 { get; set;}
            }

            public class ChildDimensions2
            {
                /// <summary>
                /// test3_FromAttribute description.
                /// </summary>
                [Dimension(""test3_FromAttribute"")]
                public string test3_WithAttribute { get; set; }

                /// <summary>
                /// test3 description.
                /// </summary>
                public string test3 { get; set; }

                /// <summary>
                /// test1_FromAttribute_In_Child2 description.
                /// </summary>
                [Dimension(""test1_FromAttribute_In_Child2"")]
                public string? test1 { get; set; }
            }

            public struct DimensionsStruct
            {
                /// <summary>
                /// testStruct_WithAttribute description.
                /// </summary>
                [Dimension(""testStruct_FromAttribute"")]
                public string testStruct_WithAttribute { get; set; }

                /// <summary>
                /// testStruct description.
                /// </summary>
                public string testStruct { get; set; }
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TestHistogram CreateTestHistogram(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StructTypeCounterWithDescription()
    {
        var d = await RunGenerator(@"
            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public struct CounterStruct
            {
                /// <summary>
                /// Dim1_FromAttribute description.
                /// </summary>
                [Dimension(""Dim1_FromAttribute"")]
                public string? Dim1 { get; set; }

                /// <summary>
                /// Dim2_FromAttribute description.
                /// </summary>
                [Dimension(""Dim2_FromAttribute"")]
                public string? Dim2;

                /// <summary>
                /// Operations_FromAttribute description.
                /// </summary>
                [Dimension(""Operations_FromAttribute"")]
                public Operations Operations { get; set; }
            }

            public static partial class MetricClass
            {
                [Counter(typeof(CounterStruct), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }
        ");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StructTypeHistogramWithDescription()
    {
        var d = await RunGenerator(@"
            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public struct HistogramStruct
            {
                /// <summary>
                /// Dim1_FromAttribute description.
                /// </summary>
                [Dimension(""Dim1_FromAttribute"")]
                public string? Dim1 { get; set; }

                /// <summary>
                /// Operations_FromAttribute description.
                /// </summary>
                [Dimension(""Operations_FromAttribute"")]
                public Operations Operations { get; set; }
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(HistogramStruct), Name=""TotalCountTest"")]
                public static partial TotalHistogram CreateTotalHistogram(Meter meter);
            }");

        Assert.Empty(d);
    }
}
