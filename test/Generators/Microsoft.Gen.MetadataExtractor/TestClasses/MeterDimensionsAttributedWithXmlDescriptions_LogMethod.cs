// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Test{

public class LogMethod
{
    [LoggerMessage("Hello {user}")]
    public void LogHello([C2(Notes = "Note 3")] string user, int port);
}
}


namespace TestClasses
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "For testing emitter for classes with description for metrics.")]
        Justification = "Metrics generator tests")]
    internal static partial class MeterDimensionsAttributedWithXmlDescriptions
    {
        public const string Dim1 = "Dim1";

        [Counter(DescriptedDimensions.Dimension1, Dim1)]
        public static partial DescribedDimensionCounter CreatePublicCounter(Meter meter);

        /// <summary>
        /// DimensionDefinedInMetricClass description.
        /// </summary>
        public const string DimensionDefinedInMetricClass = "DimensionDefinedInMetricClass";

        [Histogram(DescriptedDimensions.Dimension2, DimensionDefinedInMetricClass)]
        public static partial DescribedDimensionHistogram CreatePublicHistogram(Meter meter);

        [Counter(typeof(DimensionForStrongTypes), Name = "MyStrongTypeMetricWithDescription")]
        public static partial StrongTypeCounterWithDescriptedDimension CreateStrongTypeCounterWithDescribedDimensions(Meter meter);
    }

#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// DescriptedDimensions class description.
    /// </summary>
    internal static class DescriptedDimensions
    {
        /// <summary>
        /// Dimension1 description.
        /// </summary>
        public const string Dimension1 = "Dimension1";

        /// <summary>
        /// Dimension2 description.
        /// </summary>
        public const string Dimension2 = "Dimension2";

        /// <summary>
        /// Dimension3 description.
        /// </summary>
        public const string Dimension3 = "Dimension3";
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
        [TagName("Enum2")]
        public MetricOperations MetricEnum2 { get; set; }

        /// <summary>
        /// Gets or sets ChildDimensionsClass.
        /// </summary>
        public ChildClassDimensionForStrongTypes? ChildDimensionsClass { get; set; }

        /// <summary>
        /// Gets or sets ChildDimensionsStruct.
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
        [TagName("dim2FromAttribute")]
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
        [TagName("Dim5FromAttribute")]
        public string Dim5Struct { get; set; }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
