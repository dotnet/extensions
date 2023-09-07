// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Options for logging enrichment features.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class LoggerEnrichmentOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include stack traces when an exception is logged.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// When set to <see langword="true" /> and exceptions are logged, the logger will add exception stack trace
    /// with inner exception as a separate key-value pair with key 'stackTrace'. The maximum length of the column
    /// defaults to 4096 characters and can be modified by setting the <see cref="P:Microsoft.Extensions.Telemetry.Logging.LoggerEnrichmentOptions.MaxStackTraceLength" /> property.
    /// The stack trace beyond the current limit will be truncated.
    /// </remarks>
    public bool CaptureStackTraces { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to consult debugging files (PDB files) when producing stack traces.
    /// </summary>
    /// <remarks>
    /// Reading available debugging files produces richer stack traces, but can cost a substantial amount of time
    /// to generate. As a result, this option should only be turned on in development scenarios, not for production use.
    /// This defaults to <see langword="false" />.
    /// </remarks>
    public bool UseFileInfoForStackTraces { get; set; }

    /// <summary>
    /// Gets or sets the maximum stack trace length to emit for a given log record.
    /// </summary>
    /// <value>
    /// The default value is 4096.
    /// </value>
    /// <remarks>
    /// When set to a value less than 2 KB or greater than 32 KB, an exception will be thrown.
    /// </remarks>
    [Range(2048, 32768)]
    public int MaxStackTraceLength { get; set; }

    public LoggerEnrichmentOptions();
}
