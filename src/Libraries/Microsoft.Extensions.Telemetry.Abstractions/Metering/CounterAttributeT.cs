// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Metering;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Provides information to guide the production of a strongly-typed 64 bit integer counter metric factory method and associated type.
/// </summary>
/// <typeparam name="T">
/// The type of value the counter will hold, which is limited to <see cref="byte"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>,
/// <see cref="float"/>, <see cref="double"/>, or <see cref="decimal"/>.
/// </typeparam>
/// <remarks>
/// This attribute is applied to a method which has the following constraints:
/// <list type="bullet">
/// <item>Must be a partial method.</item>
/// <item>Must return <c>metricName</c> as the type. A class with that name will be generated.</item>
/// <item>Must not be generic.</item>
/// <item>Must have <c>System.Diagnostics.Metrics.Meter</c> as first parameter.</item>
/// <item>Must have all the keys provided in <c>staticDimensions</c> as string type parameters.</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// static partial class Metric
/// {
///     [Counter&lt;int&gt;("RequestName", "RequestStatusCode")]
///     static partial RequestCounter CreateRequestCounter(Meter meter);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class CounterAttribute<T> : Attribute
    where T : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CounterAttribute{T}"/> class.
    /// </summary>
    /// <param name="dimensions">variable array of dimension names.</param>
    public CounterAttribute(params string[] dimensions)
    {
        Dimensions = dimensions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CounterAttribute{T}"/> class.
    /// </summary>
    /// <param name="type">A type providing the metric dimensions. The dimensions are taken from the type's public fields and properties.</param>
    public CounterAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets or sets the name of the metric.
    /// </summary>
    /// <example>
    /// <code>
    /// static partial class Metric
    /// {
    ///     [Counter&lt;int&gt;("RequestName", "RequestStatusCode", Name="SampleMetric")]
    ///     static partial RequestCounter CreateRequestCounter(Meter meter);
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// In this example the metric name is <c>SampleMetric</c>. When <c>Name</c> is not provided
    /// the return type of the method is used as metric name. In this example, this would
    /// be <c>RequestCounter</c> if <c>Name</c> wasn't provided.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the metric's dimensions.
    /// </summary>
    public string[]? Dimensions { get; }

    /// <summary>
    /// Gets the type that supplies metric dimensions.
    /// </summary>
    public Type? Type { get; }
}
