// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

public class RequestHeadersLogEnricherOptions
{
    [Required]
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public IDictionary<string, DataClassification> HeadersDataClasses { get; set; }
    public RequestHeadersLogEnricherOptions();
}
