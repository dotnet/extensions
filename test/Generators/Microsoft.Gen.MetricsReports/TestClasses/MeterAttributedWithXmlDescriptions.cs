// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metrics;

namespace TestClasses
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "For testing emitter for classes with description for metrics.")]
    internal static partial class MeterAttributedWithXmlDescriptions
    {
        /// <summary>
        /// InClassDim description.
        /// </summary>
        private const string InClassDimensionName = "InClassDim";

        /// <summary>
        /// CounterWithDescription description.
        /// </summary>
        /// <param name="meter"></param>
        /// <returns></returns>
        [Counter]
        public static partial CounterWithDescription CreateDescribedCounter(Meter meter);

        /// <summary>
        /// HistogramWithDescription description.
        /// </summary>
        /// <param name="meter"></param>
        /// <returns></returns>
        [Histogram]
        public static partial HistogramWithDescription CreateDescribedHistogram(Meter meter);

        /// no xml tags
        [Histogram]
        public static partial HistogramWithWrongDescription CreateWrongDescribedHistogram(Meter meter);

        /// <summary>
        /// CreateConstDescribedCounter description.
        /// </summary>
        /// <param name="meter"></param>
        /// <returns></returns>
        [Counter(MetricConstants.DimWithXmlComment, InClassDimensionName)]
        public static partial ConstDescribedCounter CreateConstDescribedCounter(Meter meter);
    }

    internal static class MetricConstants
    {
        /// <summary>
        /// Dim4 description.
        /// </summary>
        public const string DimWithXmlComment = "Dim4";
    }
}
