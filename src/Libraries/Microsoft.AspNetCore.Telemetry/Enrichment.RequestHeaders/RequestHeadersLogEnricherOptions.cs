// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Options for the Request Headers enricher.
/// </summary>
public class RequestHeadersLogEnricherOptions
{
    /// <summary>
    /// Gets or sets a dictionary of header names that logs should be enriched with and their data classification.
    /// </summary>
    /// <remarks>
    /// Default value is an empty dictionary.
    /// </remarks>
    [Required]
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IDictionary<string, DataClassification> HeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();
#pragma warning restore CA2227 // Collection properties should be read only
}
