// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class TagNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagNameAttribute"/> class.
    /// </summary>
    /// <param name="name">Tag name.</param>
    public TagNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string Name { get; }
}
