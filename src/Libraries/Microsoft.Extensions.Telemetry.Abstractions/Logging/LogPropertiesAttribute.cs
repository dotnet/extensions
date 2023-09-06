// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Marks a logging method parameter whose public tags need to be logged.
/// </summary>
/// <seealso cref="LoggerMessageAttribute"/>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertiesAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether <see langword="null"/> tags are logged.
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
}
