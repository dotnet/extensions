// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

public class LoggingOptions
{
    public bool LogRequestStart { get; set; }
    public bool LogBody { get; set; }
    [Range(1, 1572864)]
    public int BodySizeLimit { get; set; }
    [TimeSpan(1, 3600000)]
    public TimeSpan BodyReadTimeout { get; set; }
    [Required]
    public ISet<string> RequestBodyContentTypes { get; set; }
    [Required]
    public ISet<string> ResponseBodyContentTypes { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; }
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; }
    public OutgoingPathLoggingMode RequestPathLoggingMode { get; set; }
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; }
    public LoggingOptions();
}
