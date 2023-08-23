// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Top-level model for redacting incoming HTTP requests and their corresponding responses.
/// </summary>
[Experimental("ID")]
public class LoggingRedactionOptions
{
    private const IncomingPathLoggingMode DefaultRequestPathLoggingMode = IncomingPathLoggingMode.Formatted;
    private const HttpRouteParameterRedactionMode DefaultPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;

    /// <summary>
    /// Gets or sets a strategy how request path should be logged.
    /// </summary>
    /// <remarks>
    /// Make sure you add redactors to ensure that sensitive information doesn't find its way into your log records.
    /// Default set to <see cref="IncomingPathLoggingMode.Formatted"/>.
    /// This option only applies when the <see cref="RequestPathParameterRedactionMode"/>
    /// option is not set to <see cref="HttpRouteParameterRedactionMode.None"/>.
    /// </remarks>
    public IncomingPathLoggingMode RequestPathLoggingMode { get; set; } = DefaultRequestPathLoggingMode;

    /// <summary>
    /// Gets or sets a value indicating how request path parameter should be redacted.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="HttpRouteParameterRedactionMode.Strict"/>.
    /// </remarks>
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; } = DefaultPathParameterRedactionMode;

    /// <summary>
    /// Gets or sets a map between HTTP path parameters and their data classification.
    /// </summary>
    /// <remarks>
    /// Default set to an empty dictionary.
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
    /// <remarks>
    /// Default set to an empty dictionary.
    /// That means that no request header will be logged by default.
    /// </remarks>
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets a map between response headers to be logged and their data classification.
    /// </summary>
    /// <remarks>
    /// Default set to an empty dictionary.
    /// That means that no response header will be logged by default.
    /// </remarks>
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>();

    /// <summary>
    /// Gets or sets the set of HTTP paths that should be excluded from logging.
    /// </summary>
    /// <remarks>
    /// Any path added to the set will not be logged.
    /// Paths are case insensitive.
    /// Default set to an empty <see cref="HashSet{T}"/>.
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
    [Required]
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public ISet<string> ExcludePathStartsWith { get; set; } = new HashSet<string>();
}

#endif
