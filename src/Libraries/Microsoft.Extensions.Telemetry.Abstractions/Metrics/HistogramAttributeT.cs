// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Metrics;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Provides information to guide the production of a strongly typed histogram metric factory method and associated type.
/// </summary>
/// <typeparam name="T">
/// The type of value the histogram will hold, which is limited to <see cref="byte"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>,
/// <see cref="float"/>, <see cref="double"/>, or <see cref="decimal"/>.
/// </typeparam>
/// <remarks>
/// This attribute is applied to a method that has the following constraints:
/// <list type="bullet">
///   <item>Must be a partial method.</item>
///   <item>Must return <c>metricName</c> as the type. A class with that name will be generated.</item>
///   <item>Must not be generic.</item>
///   <item>Must have <c>System.Diagnostics.Metrics.Meter</c> as first parameter.</item>
///   <item>Must have all the keys provided in <c>staticTags</c> as string type parameters.</item>
/// </list>
/// </remarks>
/// <example>
/// <code language="csharp">
/// static partial class Metric
/// {
///     [Histogram&lt;int&gt;("RequestName", "RequestStatusCode")]
///     static partial RequestLatency CreateRequestLatency(Meter meter);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class HistogramAttribute<T> : Attribute
    where T : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistogramAttribute{T}"/> class.
    /// </summary>
    /// <param name="tagNames">A variable array of tag names.</param>
    public HistogramAttribute(params string[] tagNames)
    {
        TagNames = tagNames;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HistogramAttribute{T}"/> class.
    /// </summary>
    /// <param name="type">A type providing the metric tag names. The tag values are taken from the type's public fields and properties.</param>
    public HistogramAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the name of the metric.
    /// </summary>
    /// <example>
    /// In this example metric name is <c>SampleMetric</c>.
    /// If <c>Name</c> wasn't passed, the metric name would be <c>RequestLatency</c>.
    /// <code language="csharp">
    /// static partial class Metric
    /// {
    ///     [Histogram&lt;int&gt;("RequestName", "RequestStatusCode", Name = "SampleMetric")]
    ///     static partial RequestLatency CreateRequestLatency(Meter meter);
    /// }
    /// </code>
    /// </example>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the metric's tag names.
    /// </summary>
    public string[]? TagNames { get; }

    /// <summary>
    /// Gets the type that supplies metric tag values.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Gets or sets the unit of measurement for the metric.
    /// </summary>
    [Experimental(DiagnosticIds.Experiments.Telemetry)]
    public string? Unit { get; set; }

    /// <summary>
    /// Gets or sets the description of the metric.
    /// </summary>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
    public string? Description { get; set; }

#if NET10_0_OR_GREATER
    /// <summary>
    /// Gets or sets the bucket boundaries the histogram advises collectors to use, as
    /// <see cref="System.Diagnostics.Metrics.InstrumentAdvice{T}.HistogramBucketBoundaries"/>.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>The values must be distinct, in increasing order and not NaN.</item>
    ///   <item>An empty array advises a histogram with no buckets, which tracks only the count and the sum.</item>
    ///   <item><see langword="null"/>, the default, leaves the buckets to the collector.</item>
    ///   <item>Available only when targeting .NET 10 or later.</item>
    ///   <item>Not available when <typeparamref name="T"/> is <see cref="decimal"/>, which attribute arguments can't hold.</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// static partial class Metric
    /// {
    ///     [Histogram&lt;double&gt;("RequestName", Unit = "s", HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.5, 1, 5, 10])]
    ///     static partial RequestDuration CreateRequestDuration(Meter meter);
    /// }
    /// </code>
    /// </example>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
    public T[]? HistogramBucketBoundaries { get; set; }
#endif
}
