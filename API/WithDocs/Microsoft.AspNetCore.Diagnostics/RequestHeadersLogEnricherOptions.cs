// Assembly 'Microsoft.AspNetCore.Telemetry'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.AspNetCore.Diagnostics;

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
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public IDictionary<string, DataClassification> HeadersDataClasses { get; set; }

    public RequestHeadersLogEnricherOptions();
}
