// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Options for logger.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include log scopes in
    /// captured log state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    public bool IncludeScopes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to format the message included in captured log state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// When set to <see langword="true" /> the placeholders in the message will be replaced by the actual values,
    /// otherwise the message template will be included as-is without replacements.
    /// </remarks>
    public bool UseFormattedMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include stack trace when exception is logged.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// When set to <see langword="true" /> and exceptions are logged, the logger will add exception stack trace
    /// with inner exception as a separate key-value pair with key 'stackTrace'. The maximum length of the column
    /// defaults to 4096 characters and can be modified by setting the <see cref="P:Microsoft.Extensions.Telemetry.Logging.LoggingOptions.MaxStackTraceLength" /> property.
    /// The stack trace beyond the current limit will be truncated.
    /// </remarks>
    public bool IncludeStackTrace { get; set; }

    /// <summary>
    /// Gets or sets the maximum stack trace length configured by the user.
    /// </summary>
    /// <value>
    /// The default value is 4096.
    /// </value>
    /// <remarks>
    /// When set to a value less than 2 KB or greater than 32 KB, an exception will be thrown.
    /// </remarks>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [Range(2048, 32768, ErrorMessage = "Maximum stack trace length should be between 2kb and 32kb")]
    public int MaxStackTraceLength { get; set; }

    public LoggingOptions();
}
