// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Provides dimension information for strongly-typed metrics.
/// </summary>
/// <remarks>
/// This attribute is applied to fields or properties of a metric class to override default dimension names. By default,
/// the dimension name is the same as the respective field or property. Using this attribute you can override the default
/// and provide a custom dimension name.
/// </remarks>
/// <example>
/// <code>
/// public class MyStrongTypeMetric
/// {
///     [Dimension("dimension_name_as_per_some_convention1")]
///     public string Dimension1 { get; set; }
///
///     [Dimension("dimension_name_as_per_some_convention2")]
///     public string Dimension2;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DimensionAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the dimension.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Metering.DimensionAttribute" /> class.
    /// </summary>
    /// <param name="name">Dimension name.</param>
    public DimensionAttribute(string name);
}
