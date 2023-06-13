// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Top-level model for formatting incoming HTTP requests and their corresponding responses.
/// </summary>
public class LoggingOptions
{
    private const int Millisecond = 1;
    private const int Minute = 60_000;
    private const int MaxBodyReadSize = 1_572_864; // 1.5 MB
    private const int DefaultBodyReadSizeLimit = 32 * 1024; // ≈ 32K
    private const IncomingPathLoggingMode DefaultRequestPathLoggingMode = IncomingPathLoggingMode.Formatted;
    private const HttpRouteParameterRedactionMode DefaultPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;

    private static readonly TimeSpan _defaultReadTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets a value indicating whether the request is logged additionally before any further processing.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
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
    /// The default value is <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Avoid enabling this option in a production environment as it might lead to leaking privacy information.
    /// </remarks>
    public bool LogBody { get; set; }

    /// <summary>
    /// Gets or sets a strategy how request path should be logged.
    /// </summary>
    /// <value>
    /// The default value is <see cref="IncomingPathLoggingMode.Formatted"/>.
    /// </value>
    /// <remarks>
    /// Make sure you add redactors to ensure that sensitive information doesn't find its way into your log records.
    /// This option only applies when the <see cref="RequestPathParameterRedactionMode"/>
    /// option is not set to <see cref="HttpRouteParameterRedactionMode.None"/>.
    /// </remarks>
    public IncomingPathLoggingMode RequestPathLoggingMode { get; set; } = DefaultRequestPathLoggingMode;

    /// <summary>
    /// Gets or sets a value indicating how request path parameter should be redacted.
    /// </summary>
    /// <value>
    /// The default value is <see cref="HttpRouteParameterRedactionMode.Strict"/>.
    /// </value>
    [Experimental]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; } = DefaultPathParameterRedactionMode;

    /// <summary>
    /// Gets or sets a maximum amount of time to wait for the request body to be read.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    /// <remarks>
    /// The number should be above 1 millisecond and below 1 minute.
    /// </remarks>
    [TimeSpan(Millisecond, Minute)]
    public TimeSpan RequestBodyReadTimeout { get; set; } = _defaultReadTimeout;

    /// <summary>
    /// Gets or sets the maximum number of bytes of the request/response body to be read.
    /// </summary>
    /// <value>
    /// The default is ≈ 32K.
    /// </value>
    /// <remarks>
    /// The number should ideally be below 85K to not be allocated on the <see href="https://learn.microsoft.com/dotnet/standard/garbage-collection/large-object-heap">large object heap</see>.
    /// </remarks>
    [Range(1, MaxBodyReadSize)]
    public int BodySizeLimit { get; set; } = DefaultBodyReadSizeLimit;

    /// <summary>
    /// Gets or sets a map between HTTP path parameters and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary.
    /// </value>
    /// <remarks>
    /// If a parameter within a controller's action is not annotated with a data classification attribute and
    /// it's not found in this map, it will be redacted as if it was <see cref="DataClassification.Unknown"/>.
    /// If you don't want a parameter to be redacted, mark it as <see cref="DataClassification.None"/>.
    /// </remarks>
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets a map between request headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no request header is logged by default.
    /// </value>
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets the set of request body content types which are considered text and thus possible to log.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="HashSet{T}"/>, which means that the request's body isn't logged.
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
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public ISet<string> RequestBodyContentTypes { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets a map between response headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no response header is logged by default.
    /// </value>
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets the set of response body content types which are considered text and thus possible to log.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="HashSet{T}"/>, which means that the response's body isn't logged.
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
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public ISet<string> ResponseBodyContentTypes { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the set of HTTP paths that should be excluded from logging.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="HashSet{T}"/>.
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
    [Experimental]
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public ISet<string> ExcludePathStartsWith { get; set; } = new HashSet<string>();
}
