// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Marks a logging method parameter whose public properties need to be logged as log tags.
/// </summary>
/// <seealso cref="LoggerMessageAttribute"/>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertiesAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether <see langword="null"/> properties are logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false"/>.
    /// </value>
    public bool SkipNullProperties { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to prefix the name of the parameter or property to the generated name of each tag being logged.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false"/>.
    /// </value>
    public bool OmitReferenceName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to transitively visit properties which are complex objects.
    /// </summary>
    /// <remarks>
    /// When logging the properties of an object, this property controls the behavior for each encountered property.
    /// When this property is <see langword="false"/>, then each property is serialized by calling <see cref="object.ToString" /> to
    /// generate a string for the property. When this property is <see langword="true"/>, then each property of any complex objects are
    /// expanded individually.
    /// </remarks>
    /// <value>
    /// Defaults to <see langword="false"/>.
    /// </value>
    [Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
    public bool Transitive { get; set; }
}
