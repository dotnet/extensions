// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class LoggerEnrichmentOptions
{
    public bool CaptureStackTraces { get; set; }
    public bool UseFileInfoForStackTraces { get; set; }
    [Range(2048, 32768)]
    public int MaxStackTraceLength { get; set; }
    public LoggerEnrichmentOptions();
}
