// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Defines the tag name to use for a logged parameter or property.
/// </summary>
/// <remarks>
/// By default, the tag name is the same as the respective parameter or property.
/// You can use this attribute to override the default and provide a custom tag name.
/// </remarks>
/// <example>
/// <code language="csharp">
/// [LoggerMessage(1, LogLevel.Information, "My custom tag name: {my.custom.tagname}")]
/// public static partial void LogMyCustomTagName(
///     this ILogger logger,
///     [TagName("my.custom.tagname")] string name);
/// </code>
/// </example>
/// <seealso cref="LoggerMessageAttribute"/>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class TagNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The tag name to use when logging the annotated parameter or property.</param>
    public TagNameAttribute(string name)
    {
        Name = Throw.IfNull(name);
    }

    /// <summary>
    /// Gets the name of the tag to be used when logging the parameter or property.
    /// </summary>
    public string Name { get; }
}
