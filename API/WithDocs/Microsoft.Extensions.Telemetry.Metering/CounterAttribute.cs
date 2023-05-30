// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Provides information to guide the production of a strongly-typed 64 bit integer counter metric factory method and associated type.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Metering.CounterAttribute" /> class.
    /// </summary>
    /// <param name="dimensions">Dimension names.</param>
    public CounterAttribute(params string[] dimensions);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Metering.CounterAttribute" /> class.
    /// </summary>
    /// <param name="type">A type providing the metric dimensions. The dimensions are taken from the type's public fields and properties.</param>
    public CounterAttribute(Type type);
}
