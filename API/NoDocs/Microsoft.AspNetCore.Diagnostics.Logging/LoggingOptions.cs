// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

public class LoggingOptions
{
    public bool LogRequestStart { get; set; }
    public bool LogBody { get; set; }
    public IncomingPathLoggingMode RequestPathLoggingMode { get; set; }
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }
    [TimeSpan(1, 60000)]
    public TimeSpan RequestBodyReadTimeout { get; set; }
    [Range(1, 1572864)]
    public int BodySizeLimit { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; }
    [Required]
    public ISet<string> RequestBodyContentTypes { get; set; }
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; }
    [Required]
    public ISet<string> ResponseBodyContentTypes { get; set; }
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [Required]
    public ISet<string> ExcludePathStartsWith { get; set; }
    public LoggingOptions();
}
