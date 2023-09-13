// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Options to configure HTTP client requests logging.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the request is logged additionally before any further processing.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// When enabled, two entries will be logged for each incoming request - one for request and one for response, if available.
    /// When disabled, only one entry will be logged for each incoming request, which includes both request and response data.
    /// </remarks>
    public bool LogRequestStart { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the HTTP request and response body are logged.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// Avoid enabling this option in a production environment as it might lead to leaking privacy information.
    /// </remarks>
    public bool LogBody { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of bytes of the request or response body to read.
    /// </summary>
    /// <value>
    /// The default value is ≈ 32K.
    /// </value>
    /// <remarks>
    /// The number should ideally be below 85000 bytes to not be allocated on the <see href="https://learn.microsoft.com/dotnet/standard/garbage-collection/large-object-heap">large object heap</see>.
    /// </remarks>
    [Range(1, 1572864)]
    public int BodySizeLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum amount of time to wait for the request or response body to be read.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    /// <remarks>
    /// The value should be in the range of 1 millisecond to 1 minute.
    /// </remarks>
    [TimeSpan(1, 3600000)]
    public TimeSpan BodyReadTimeout { get; set; }

    /// <summary>
    /// Gets or sets the list of HTTP request content types which are considered text and thus possible to serialize.
    /// </summary>
    [Required]
    public ISet<string> RequestBodyContentTypes { get; set; }

    /// <summary>
    /// Gets or sets the list of HTTP response content types which are considered text and thus possible to serialize.
    /// </summary>
    [Required]
    public ISet<string> ResponseBodyContentTypes { get; set; }

    /// <summary>
    /// Gets or sets the set of HTTP request headers to log and their respective data classes to use for redaction.
    /// </summary>
    /// <value>
    /// The default value is <see cref="T:System.Collections.Generic.HashSet`1" />.
    /// </value>
    /// <remarks>
    /// If empty, no HTTP request headers will be logged.
    /// If the data class is <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.None" />, no redaction will be done.
    /// </remarks>
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; }

    /// <summary>
    /// Gets or sets the set of HTTP response headers to log and their respective data classes to use for redaction.
    /// </summary>
    /// <value>
    /// The default value is <see cref="T:System.Collections.Generic.HashSet`1" />.
    /// </value>
    /// <remarks>
    /// If the data class is <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.None" />, no redaction will be done.
    /// If empty, no HTTP response headers will be logged.
    /// </remarks>
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how the outgoing HTTP request path should be logged.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.Extensions.Http.Telemetry.Logging.OutgoingPathLoggingMode.Formatted" />.
    /// </value>
    /// <remarks>
    /// This option is applied only when the <see cref="P:Microsoft.Extensions.Http.Telemetry.Logging.LoggingOptions.RequestPathLoggingMode" /> option is not set to <see cref="F:Microsoft.Extensions.Http.Telemetry.HttpRouteParameterRedactionMode.None" />,
    /// otherwise this setting is ignored and the unredacted HTTP request path is logged.
    /// </remarks>
    public OutgoingPathLoggingMode RequestPathLoggingMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how outgoing HTTP request path parameters should be redacted.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.Extensions.Http.Telemetry.HttpRouteParameterRedactionMode.Strict" />.
    /// </value>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }

    /// <summary>
    /// Gets or sets the route parameters to redact with their corresponding data classes to apply appropriate redaction.
    /// </summary>
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }

    public LoggingOptions();
}
