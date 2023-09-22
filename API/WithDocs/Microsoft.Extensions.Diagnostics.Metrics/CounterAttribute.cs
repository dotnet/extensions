// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Metrics;

/// <summary>
/// Provides information to guide the production of a strongly-typed 64 bit integer counter metric factory method and associated type.
/// </summary>
/// <remarks>
/// This attribute is applied to a method which has the following constraints:
/// <list type="bullet">
/// <item><description>Must be a partial method.</description></item>
/// <item><description>Must return <c>metricName</c> as the type. A class with that name will be generated.</description></item>
/// <item><description>Must not be generic.</description></item>
/// <item><description>Must have <c>System.Diagnostics.Metrics.Meter</c> as first parameter.</description></item>
/// <item><description>Must have all the keys provided in <c>staticTagNames</c> as string type parameters.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// static partial class Metric
/// {
///     [Counter("RequestName", "RequestStatusCode")]
///     static partial RequestCounter CreateRequestCounter(Meter meter);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class CounterAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the metric.
    /// </summary>
    /// <example>
    /// <code>
    /// static partial class Metric
    /// {
    ///     [Counter("RequestName", "RequestStatusCode", Name="SampleMetric")]
    ///     static partial RequestCounter CreateRequestCounter(Meter meter);
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// In this example, the metric name is <c>SampleMetric</c>. When <c>Name</c> is not provided
    /// the return type of the method is used as metric name. In this example, this would
    /// be <c>RequestCounter</c> if <c>Name</c> wasn't provided.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the metric's tag names.
    /// </summary>
    public string[]? TagNames { get; }

    /// <summary>
    /// Gets the type that supplies metric tags values.
    /// </summary>
    public Type? Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.CounterAttribute" /> class.
    /// </summary>
    /// <param name="tagNames">Dimension names.</param>
    public CounterAttribute(params string[] tagNames);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.CounterAttribute" /> class.
    /// </summary>
    /// <param name="type">A type providing the metric tag names. The tag values are taken from the type's public fields and properties.</param>
    public CounterAttribute(Type type);
}
