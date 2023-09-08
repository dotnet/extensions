// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Options for the blotting redactor.
/// </summary>
[Experimental(diagnosticId: Experiments.Compliance, UrlFormat = Experiments.UrlFormat)]
public class BlottingRedactorOptions
{
    /// <summary>
    /// Gets or sets the character to use to blot out redacted content.
    /// </summary>
    /// <value>
    /// This defaults to '*'.
    /// </value>
    public char BlottingCharacter { get; set; } = '*';
}
