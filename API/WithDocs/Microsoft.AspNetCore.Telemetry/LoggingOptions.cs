// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Top-level model for formatting incoming HTTP requests and their corresponding responses.
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
    /// When enabled, two entries will be logged for each incoming request. Note, that the first log record won't be enriched.
    /// When disabled, only one entry will be logged for each incoming request (with corresponding response's data).
    /// </remarks>
    public bool LogRequestStart { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTTP request and response body will be logged.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// Avoid enabling this option in a production environment as it might lead to leaking privacy information.
    /// </remarks>
    public bool LogBody { get; set; }

    /// <summary>
    /// Gets or sets a strategy how request path should be logged.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.AspNetCore.Telemetry.IncomingPathLoggingMode.Formatted" />.
    /// </value>
    /// <remarks>
    /// Make sure you add redactors to ensure that sensitive information doesn't find its way into your log records.
    /// This option only applies when the <see cref="P:Microsoft.AspNetCore.Telemetry.LoggingOptions.RequestPathParameterRedactionMode" />
    /// option is not set to <see cref="F:Microsoft.Extensions.Http.Telemetry.HttpRouteParameterRedactionMode.None" />.
    /// </remarks>
    public IncomingPathLoggingMode RequestPathLoggingMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how request path parameter should be redacted.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.Extensions.Http.Telemetry.HttpRouteParameterRedactionMode.Strict" />.
    /// </value>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }

    /// <summary>
    /// Gets or sets a maximum amount of time to wait for the request body to be read.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    /// <remarks>
    /// The value should be in the range of 1 millisecond to 1 minute.
    /// </remarks>
    [TimeSpan(1, 60000)]
    public TimeSpan RequestBodyReadTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of bytes of the request/response body to be read.
    /// </summary>
    /// <value>
    /// The default is ≈ 32K.
    /// </value>
    /// <remarks>
    /// The number should ideally be below 85000 bytes to not be allocated on the <see href="https://learn.microsoft.com/dotnet/standard/garbage-collection/large-object-heap">large object heap</see>.
    /// </remarks>
    [Range(1, 1572864)]
    public int BodySizeLimit { get; set; }

    /// <summary>
    /// Gets or sets a map between HTTP path parameters and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary.
    /// </value>
    /// <remarks>
    /// If a parameter within a controller's action is not annotated with a data classification attribute and
    /// it's not found in this map, it will be redacted as if it was <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown" />.
    /// If you don't want a parameter to be redacted, mark it as <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.None" />.
    /// </remarks>
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }

    /// <summary>
    /// Gets or sets a map between request headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no request header is logged by default.
    /// </value>
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; }

    /// <summary>
    /// Gets or sets the set of request body content types which are considered text and thus possible to log.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="T:System.Collections.Generic.HashSet`1" />, which means that the request's body isn't logged.
    /// </value>
    /// <remarks>
    /// Don't enable body logging in a production environment, as it might impact
    /// performance and leak sensitive data.
    /// If you need to log body in production, go through compliance and security.
    /// </remarks>
    /// <example>
    /// A typical set of known text content-types like json, xml or text would be:
    /// <code>
    /// RequestBodyContentTypesToLog = new HashSet&lt;string&gt;
    /// {
    ///     "application/*+json",
    ///     "application/*+xml",
    ///     "application/json",
    ///     "application/xml",
    ///     "text/*"
    /// };
    /// </code>
    /// </example>
    [Required]
    public ISet<string> RequestBodyContentTypes { get; set; }

    /// <summary>
    /// Gets or sets a map between response headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no response header is logged by default.
    /// </value>
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; }

    /// <summary>
    /// Gets or sets the set of response body content types which are considered text and thus possible to log.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="T:System.Collections.Generic.HashSet`1" />, which means that the response's body isn't logged.
    /// </value>
    /// <remarks>
    /// Don't enable body logging in a production environment, as it might impact performance and leak sensitive data.
    /// If you need to log body in production, go through compliance and security.
    /// </remarks>
    /// <example>
    /// A typical set of known text content-types like json, xml or text would be:
    /// <code>
    /// ResponseBodyContentTypesToLog = new HashSet&lt;string&gt;
    /// {
    ///     "application/*+json",
    ///     "application/*+xml",
    ///     "application/json",
    ///     "application/xml",
    ///     "text/*"
    /// };
    /// </code>
    /// </example>
    [Required]
    public ISet<string> ResponseBodyContentTypes { get; set; }

    /// <summary>
    /// Gets or sets the set of HTTP paths that should be excluded from logging.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="T:System.Collections.Generic.HashSet`1" />.
    /// </value>
    /// <remarks>
    /// Any path added to the set will not be logged.
    /// Paths are case insensitive.
    /// </remarks>
    /// <example>
    /// A typical set of HTTP paths would be:
    /// <code>
    /// ExcludePathStartsWith = new HashSet&lt;string&gt;
    /// {
    ///     "/probe/live",
    ///     "/probe/ready"
    /// };
    /// </code>
    /// </example>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [Required]
    public ISet<string> ExcludePathStartsWith { get; set; }

    public LoggingOptions();
}
