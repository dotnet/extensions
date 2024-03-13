// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

public partial class ParserTests
{
    [Fact]
    public async Task StrongTypeCounter_CyclicReference()
    {
        var d = await RunGenerator(@"
            public class TypeA
            {
                public TypeB testB { get; set; }
            }

            public class TypeB
            {
                public TypeA testA { get; set; }
            }

            public static partial class MetricClass
            {
                [Counter(typeof(TypeA), Name=""CyclicTest"")]
                public static partial CyclicTest CreateCyclicTestCounter(Meter meter);
            }");

        Assert.NotNull(d);
        var diag = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorTagTypeCycleDetected.Id, diag.Id);
        Assert.Contains("Test.TypeB ⇆ Test.TypeA", diag.GetMessage());
    }

    [Fact]
    public async Task StrongTypeCounter_CyclicReference_BaseCycle()
    {
        var d = await RunGenerator(@"
            public class TypeA : TypeB
            {
            }

            public class TypeB
            {
                public TypeC testC { get; set; }
            }

            public class TypeC
            {
                public TypeB testB { get; set; }
            }

            public static partial class MetricClass
            {
                [Counter(typeof(TypeA), Name=""CyclicTest"")]
                public static partial CyclicTest CreateCyclicTestCounter(Meter meter);
            }");

        Assert.NotNull(d);
        var diag = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorTagTypeCycleDetected.Id, diag.Id);
        Assert.Contains("Test.TypeC ⇆ Test.TypeB", diag.GetMessage());
    }

    [Fact]
    public async Task StrongTypeCounter_CyclicReference_InterimCycle()
    {
        var d = await RunGenerator(@"
            internal static partial class Metric
            {
                [Counter(typeof(Interim), Name=""CyclicTest"")]
                public static partial CyclicTest CreateCyclicTestCounter(Meter meter);
            }

            class BaseClass
            {
                public Transitive ToTransitive { get; set; }
            }

            class Interim : BaseClass
            {
                public Transitive ToTransitive { get; set; }
            }

            class Transitive
            {
                public Interim ToInterim { get; set; }
            }");

        Assert.NotNull(d);
        var diag = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorTagTypeCycleDetected.Id, diag.Id);
        Assert.Contains("Test.Transitive ⇆ Test.Interim", diag.GetMessage());
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

        Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorGaugeNotSupported.Id, d[0].Id);
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

        Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorGaugeNotSupported.Id, d[0].Id);
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
        Assert.Equal(DiagDescriptors.ErrorDuplicateTagName.Id, d[0].Id);
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
        Assert.Equal(DiagDescriptors.ErrorDuplicateTagName.Id, d[0].Id);
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
        Assert.Equal(DiagDescriptors.ErrorDuplicateTagName.Id, d[0].Id);
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
        Assert.Equal(DiagDescriptors.ErrorDuplicateTagName.Id, d[0].Id);
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
        Assert.Equal(DiagDescriptors.ErrorInvalidTagNameType.Id, d[0].Id);
    }

    [Fact]
    public async Task TooManyDimensions()
    {
        StringBuilder sb = new StringBuilder();

        int i = 0;

        for (; i < 30; i++)
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
        Assert.Equal(DiagDescriptors.ErrorTooManyTagNames.Id, d[0].Id);
    }
}
