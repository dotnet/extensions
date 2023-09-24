// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Metrics;

/// <summary>
/// Provides tag information for strongly-typed metrics.
/// </summary>
/// <remarks>
/// This attribute is applied to fields or properties of a metric class to override default tag names. By default,
/// the tag name is the same as the respective field or property. Using this attribute you can override the default
/// and provide a custom tag name.
/// </remarks>
/// <example>
/// <code>
/// public class MyStrongTypeMetric
/// {
///     [TagName("tag_name_as_per_some_convention1")]
///     public string Name1 { get; set; }
///
///     [TagName("tag_name_as_per_some_convention2")]
///     public string Name2;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TagNameAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Metrics.TagNameAttribute" /> class.
    /// </summary>
    /// <param name="name">Tag name.</param>
    public TagNameAttribute(string name);
}
