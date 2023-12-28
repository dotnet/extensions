// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Defines the tag name to use for a logged parameter or property.
/// </summary>
/// <seealso cref="LoggerMessageAttribute"/>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
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
