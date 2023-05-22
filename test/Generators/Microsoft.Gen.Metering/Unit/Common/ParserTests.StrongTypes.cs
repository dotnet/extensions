// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
            }");

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
    public async Task StructTypeGauge()
    {
        var d = await RunGenerator(@"
            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public struct GaugeStruct
            {
                [Dimension(""Dim1_FromAttribute"")]
                public string? Dim1 { get; set; }

                [Dimension(""Operations_FromAttribute"")]
                public Operations Operations { get; set; }
            }

            public static partial class MetricClass
            {
                [Gauge(typeof(GaugeStruct), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task StrongTypeGauge()
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
                [Gauge(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task DuplicateDimensionStringName()
    {
        var d = await RunGenerator(@"
            public class DimensionsTest
            {
                public string dim1 { get; set; }
                public ChildDimensions childDimensions { get; set; }
            }

            public class ChildDimensions
            {
                public string dim1 {get;set;}
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorDuplicateDimensionName.Id, d[0].Id);
    }

    [Fact]
    public async Task DuplicateDimensionStringNameInAttribute()
    {
        var d = await RunGenerator(@"
            public class DimensionsTest
            {
                [Dimension(""dim1FromAttribute"")]
                public string dim1 { get; set; }
                public ChildDimensions childDimensions { get; set; }
            }

            public class ChildDimensions
            {
                [Dimension(""dim1FromAttribute"")]
                public string dim1 {get;set;}
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorDuplicateDimensionName.Id, d[0].Id);
    }

    [Fact]
    public async Task DuplicateDimensionEnumName()
    {
        var d = await RunGenerator(@"
            public class DimensionsTest
            {
                public Operations operations { get; set; }
                public ChildDimensions childDimensions { get; set; }
            }

            public class ChildDimensions
            {
                public Operations operations { get; set; }
            }

            public enum Operations
            {
                Unknown = 0,
                Operation1 = 1,
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorDuplicateDimensionName.Id, d[0].Id);
    }

    [Fact]
    public async Task DuplicateDimensionEnumNameInAttribute()
    {
        var d = await RunGenerator(@"
            public class DimensionsTest
            {
                [Dimension(""operations"")]
                public Operations operations { get; set; }
                public ChildDimensions childDimensions { get; set; }
            }

            public class ChildDimensions
            {
                public Operations operations { get; set; }
            }

            public enum Operations
            {
                Unknown = 0
            }

            public static partial class MetricClass
            {
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorDuplicateDimensionName.Id, d[0].Id);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("int?")]
    [InlineData("System.Int32")]
    [InlineData("System.Int32?")]
    [InlineData("bool")]
    [InlineData("bool?")]
    [InlineData("System.Boolean")]
    [InlineData("System.Boolean?")]
    [InlineData("byte")]
    [InlineData("byte?")]
    [InlineData("char?")]
    [InlineData("double?")]
    [InlineData("decimal?")]
    [InlineData("object")]
    [InlineData("object?")]
    [InlineData("System.Object")]
    [InlineData("System.Object?")]
    [InlineData("int[]")]
    [InlineData("int?[]")]
    [InlineData("int[]?")]
    [InlineData("int?[]?")]
    [InlineData("object[]")]
    [InlineData("object[]?")]
    [InlineData("System.Array")]
    [InlineData("System.DateTime")]
    [InlineData("System.DateTime?")]
    [InlineData("System.IDisposable")]
    [InlineData("System.Action")]
    [InlineData("System.Action<int>")]
    [InlineData("System.Func<double>")]
    [InlineData("System.Nullable<int>")]
    [InlineData("System.Nullable<char>")]
    [InlineData("System.Nullable<System.Int32>")]
    [InlineData("System.Nullable<System.Decimal>")]
    [InlineData("System.Nullable<System.DateTime>")]
    public async Task InvalidDimensionType(string type)
    {
        var d = await RunGenerator(@$"
            public class DimensionsTest
            {{
                public {type} dim1 {{ get; set; }}
            }}

            public static partial class MetricClass
            {{
                [Histogram(typeof(DimensionsTest), Name=""TotalCountTest"")]
                public static partial TotalCount CreateTotalCountCounter(Meter meter);
            }}");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidDimensionType.Id, d[0].Id);
    }

    [Fact]
    public async Task TooManyDimensions()
    {
        StringBuilder sb = new StringBuilder();

        int i = 0;

        for (; i < 21; i++)
        {
            sb.AppendLine($"public class C{i} : C{i + 1} {{ public string dim{i} {{get;set;}}}}");
        }

        sb.AppendLine($"public class C{i} {{ public string dim{i} {{get;set;}}}}");

        sb.AppendLine(@"        public static partial class MetricClass
        {
            [Histogram(typeof(C0), Name=""TotalCountTest"")]
            public static partial TotalCount CreateTotalCountCounter(Meter meter);
        }");

        var d = await RunGenerator(sb.ToString());

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorTooManyDimensions.Id, d[0].Id);
    }
}
