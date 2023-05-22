// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Options to configure HTTP client requests logging.
/// </summary>
public class LoggingOptions
{
    private const int MaxIncomingBodySize = 1_572_864; // 1.5 MB
    private const int Millisecond = 1;
    private const int Hour = 60000 * 60; // 1 hour
    private const int DefaultReadSizeLimit = 32 * 1024;  // ≈ 32K
    private const OutgoingPathLoggingMode DefaultPathLoggingMode = OutgoingPathLoggingMode.Formatted;
    private const HttpRouteParameterRedactionMode DefaultPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;

    /// <summary>
    /// Gets or sets a value indicating whether request will be logged additionally before any further processing.
    /// </summary>
    /// <remarks>
    /// When enabled, two entries will be logged for each incoming request - one for request and one for response, if available.
    /// When disabled, only one entry will be logged for each incoming request which includes both request and response data.
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool LogRequestStart { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTTP request and response body will be logged.
    /// </summary>
    /// <remarks>
    /// Please avoid enabling this options in production environment as it might lead to leaking privacy information.
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool LogBody { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the maximum number of bytes of the request or response body to read.
    /// </summary>
    /// <remarks>
    /// The number should ideally be below 85K to not be allocated on the <see href="https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap">large object heap</see>.
    /// Default set to ≈ 32K.
    /// </remarks>
    [Range(1, MaxIncomingBodySize)]
    public int BodySizeLimit { get; set; } = DefaultReadSizeLimit;

    /// <summary>
    /// Gets or sets a value indicating the maximum amount of time to wait for the request or response body to be read.
    /// </summary>
    /// <remarks>
    /// The number should be above 1 millisecond and below 1 hour.
    /// Default set to 1 second.
    /// </remarks>
    [TimeSpan(Millisecond, Hour)]
    public TimeSpan BodyReadTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the list of HTTP request content types which are considered text and thus possible to serialize.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    [Required]
    public ISet<string> RequestBodyContentTypes { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the list of HTTP response content types which are considered text and thus possible to serialize.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    [Required]
    public ISet<string> ResponseBodyContentTypes { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the set of HTTP request headers to log and their respective data classes to use for redaction.
    /// </summary>
    /// <remarks>
    /// If empty, no HTTP request headers will be logged.
    /// If the data class is <see cref="DataClassification.None"/>, no redaction will be done.
    /// Default set to <see cref="HashSet{T}"/>.
    /// </remarks>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets the set of HTTP response headers to log and their respective data classes to use for redaction.
    /// </summary>
    /// <remarks>
    /// If the data class is <see cref="DataClassification.None"/>, no redaction will be done.
    /// If empty, no HTTP response headers will be logged.
    /// Default set to <see cref="HashSet{T}"/>.
    /// </remarks>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets a value indicating how outgoing HTTP request path should be logged.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="OutgoingPathLoggingMode.Formatted"/>.
    /// This option is applied only when the <see cref="RequestPathLoggingMode"/> option is not set to <see cref="HttpRouteParameterRedactionMode.None"/>,
    /// otherwise this setting is ignored and unredacted HTTP request path is logged.
    /// </remarks>
    public OutgoingPathLoggingMode RequestPathLoggingMode { get; set; } = DefaultPathLoggingMode;

    /// <summary>
    /// Gets or sets a value indicating how outgoing HTTP request path parameters should be redacted.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="HttpRouteParameterRedactionMode.Strict"/>.
    /// </remarks>
    [Experimental]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; } = DefaultPathParameterRedactionMode;

    /// <summary>
    /// Gets the route parameters to redact with their corresponding data classes to apply appropriate redaction.
    /// </summary>
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; } = new Dictionary<string, DataClassification>();
}
