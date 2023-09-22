// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging;

public class LoggerEnrichmentOptions
{
    public bool CaptureStackTraces { get; set; }
    public bool UseFileInfoForStackTraces { get; set; }
    public bool IncludeExceptionMessageInStackTraces { get; set; }
    [Range(2048, 32768)]
    public int MaxStackTraceLength { get; set; }
    public LoggerEnrichmentOptions();
}
