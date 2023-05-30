// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

public class HttpClientTracingOptions
{
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }
    public HttpClientTracingOptions();
}
