// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;


namespace Test{

interface IBar
{
    [C4]
    public int P0 { get; }
}

public record RecordProperty([C2] string F0, string F1, [C3] int F2) : IBar
{
    [C2(Notes = "Note 1")]
    public int F3;

    [C2(Notes = null!)]
    public int F4;

    [C3(Notes = "Note 3")]
    public int P0 { get; };

    [C3]
    public int P1 { get; };

    [LoggerMessage("Hello {user}")]
    public void LogHello([C3(Notes = "Note 3")] string user, int port);

    [LoggerMessage("World {user}")]
    public void LogWorld([C2] string user, int port);
}

[C1]
public record DerivedRecordProperty : RecordProperty
{
    [C2(Notes = "Note 2")]
    public override int P0 { get; };
}
}
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
