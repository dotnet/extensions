// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Metrics;

namespace TestClasses
{
#pragma warning disable SA1402 // File may only contain a single type
    public class CounterDimensions : CounterParentDimensions
    {
        public string? Dim1;

        public CounterOperations OperationsEnum { get; set; }

        [TagName("Enum2")]
        public CounterOperations OperationsEnum2 { get; set; }

        public CounterChildDimensions? ChildDimensionsObject { get; set; }

        public CounterGrandChildCounterDimensions? GrandChildDimensionsObject { get; set; }
    }

    public enum CounterOperations
    {
        Unknown = 0,
        Operation1 = 1,
    }

    public class CounterParentDimensions
    {
        public string? ParentOperationName { get; set; }

        public CounterDimensionsStruct ChildDimensionsStruct { get; set; }
    }

    public class CounterChildDimensions
    {
        public string? Dim2 { get; set; }

        [TagName("dim2FromAttribute")]
        public string? SomeDim;
    }

    public struct CounterDimensionsStruct
    {
        public string Dim4Struct { get; set; }

        [TagName("Dim5FromAttribute")]
        public string Dim5Struct { get; set; }
    }

    public class CounterGrandChildCounterDimensions
    {
        public string? Dim3 { get; set; }

        [TagName("Dim3FromAttribute")]
        public string? SomeDim { get; set; }
    }

    public struct CounterStructDimensions
    {
        public string? Dim1 { get; set; }

        [TagName("DimInField")]
        public string? DimInField;

        [TagName("Dim2_FromAttribute")]
        public string? Dim2 { get; set; }

        public CounterOperations Operations { get; set; }

        [TagName("Operations_FromAttribute")]
        public CounterOperations Operations2 { get; set; }
    }

    public record class CounterRecordClassDimensions
    {
        public string? Dim1 { get; set; }

        [TagName("DimInField")]
        public string? DimInField;

        [TagName("Dim2_FromAttribute")]
        public string? Dim2 { get; set; }

        public CounterOperations Operations { get; set; }

        [TagName("Operations_FromAttribute")]
        public CounterOperations Operations2 { get; set; }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
