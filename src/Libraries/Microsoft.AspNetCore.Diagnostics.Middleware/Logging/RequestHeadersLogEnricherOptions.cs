// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Options for the Request Headers enricher.
/// </summary>
public class RequestHeadersLogEnricherOptions
{
    /// <summary>
    /// Gets a dictionary of header names that logs should be enriched with and their data classification.
    /// </summary>
    /// <remarks>
    /// Default value is an empty dictionary.
    /// </remarks>
    [Required]
    [Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
    public IDictionary<string, DataClassification> HeadersDataClasses { get; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);
}
