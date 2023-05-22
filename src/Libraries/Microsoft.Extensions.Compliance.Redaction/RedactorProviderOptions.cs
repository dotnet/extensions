// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Redactor provider options.
/// </summary>
internal sealed class RedactorProviderOptions
{
    /// <summary>
    /// Gets or sets the fallback redactor to use when no classification-specific redactor exists.
    /// </summary>
    public Type FallbackRedactor { get; set; } = typeof(ErasingRedactor);

    /// <summary>
    /// Gets a dictionary of classification-specific redactors.
    /// </summary>
    public Dictionary<DataClassification, Type> Redactors { get; } = new();
}
