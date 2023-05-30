// Assembly 'Microsoft.AspNetCore.Telemetry'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.AspNetCore.Telemetry;

public class HttpTracingOptions
{
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }
    public bool IncludePath { get; set; }
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }
    [Required]
    public ISet<string> ExcludePathStartsWith { get; set; }
    public HttpTracingOptions();
}
