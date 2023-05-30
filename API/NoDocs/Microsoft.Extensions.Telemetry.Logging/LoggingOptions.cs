// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

public class LoggingOptions
{
    public bool IncludeScopes { get; set; }
    public bool UseFormattedMessage { get; set; }
    public bool IncludeStackTrace { get; set; }
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [Range(2048, 32768, ErrorMessage = "Maximum stack trace length should be between 2kb and 32kb")]
    public int MaxStackTraceLength { get; set; }
    public LoggingOptions();
}
