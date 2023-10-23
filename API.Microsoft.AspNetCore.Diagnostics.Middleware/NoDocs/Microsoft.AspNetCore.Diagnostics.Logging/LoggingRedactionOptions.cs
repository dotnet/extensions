// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

[Experimental("EXTEXP0013", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class LoggingRedactionOptions
{
    public IncomingPathLoggingMode RequestPathLoggingMode { get; set; }
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; }
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; }
    [Required]
    public ISet<string> ExcludePathStartsWith { get; set; }
    public LoggingRedactionOptions();
}
