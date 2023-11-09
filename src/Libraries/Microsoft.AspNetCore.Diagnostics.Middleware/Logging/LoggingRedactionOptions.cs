// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Top-level model for redacting incoming HTTP requests and their corresponding responses.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.HttpLogging, UrlFormat = DiagnosticIds.UrlFormat)]
public class LoggingRedactionOptions
{
    private const IncomingPathLoggingMode DefaultRequestPathLoggingMode = IncomingPathLoggingMode.Formatted;
    private const HttpRouteParameterRedactionMode DefaultPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;

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
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; } = DefaultPathParameterRedactionMode;

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
#pragma warning disable CA2227 // Collection properties should be read only
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets a map between request headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no request header is logged by default.
    /// </value>
    [Required]
#pragma warning disable CA2227 // Collection properties should be read only
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets a map between response headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no response header is logged by default.
    /// </value>
    [Required]
#pragma warning disable CA2227 // Collection properties should be read only
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227 // Collection properties should be read only

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
    /// - "/probe/live".
    /// - "/probe/ready".
    /// </example>
    [Required]
#pragma warning disable CA2227 // Collection properties should be read only
    public ISet<string> ExcludePathStartsWith { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore CA2227 // Collection properties should be read only
}

#endif
