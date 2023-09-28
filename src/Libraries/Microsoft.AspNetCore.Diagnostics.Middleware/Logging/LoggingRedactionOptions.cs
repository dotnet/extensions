// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Top-level model for redacting incoming HTTP requests and their corresponding responses.
/// </summary>
[Experimental(diagnosticId: Experiments.HttpLogging, UrlFormat = Experiments.UrlFormat)]
public class LoggingRedactionOptions
{
    private const IncomingPathLoggingMode DefaultRequestPathLoggingMode = IncomingPathLoggingMode.Formatted;
    private const HttpRouteParameterRedactionMode DefaultPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingRedactionOptions"/> class.
    /// </summary>
    public LoggingRedactionOptions()
    {
        // The collection's constructor is internal.
        MediaTypeOptions = new HttpLoggingOptions().MediaTypeOptions;
        MediaTypeOptions.Clear();
    }

    /// <summary>
    /// Gets or sets the fields to log for the Request and Response. Defaults to logging request and response properties, headers, and duration.
    /// </summary>
    public HttpLoggingFields LoggingFields { get; set; } = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.Duration;

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
    /// Gets a map between HTTP path parameters and their data classification.
    /// </summary>
    /// <remarks>
    /// Default set to an empty dictionary.
    /// If a parameter within a controller's action is not annotated with a data classification attribute and
    /// it's not found in this map, it will be redacted as if it was <see cref="DataClassification.Unknown"/>.
    /// If you don't want a parameter to be redacted, mark it as <see cref="DataClassification.None"/>.
    /// </remarks>
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a map between request headers to be logged and their data classification.
    /// </summary>
    /// <remarks>
    /// Default set to an empty dictionary.
    /// That means that no request header will be logged by default.
    /// </remarks>
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a map between response headers to be logged and their data classification.
    /// </summary>
    /// <remarks>
    /// Default set to an empty dictionary.
    /// That means that no response header will be logged by default.
    /// </remarks>
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; } = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the set of HTTP paths that should be excluded from logging.
    /// </summary>
    /// <remarks>
    /// Any path added to the set will not be logged.
    /// Paths are case insensitive.
    /// Default set to an empty <see cref="HashSet{T}"/>.
    /// </remarks>
    /// <example>
    /// A typical set of HTTP paths would be:
    /// - "/probe/live".
    /// - "/probe/ready".
    /// </example>
    public ISet<string> ExcludePathStartsWith { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the set of request body content types which are possible to log.
    /// </summary>
    /// <value>
    /// The default value is empty which means that the request's body isn't logged.
    /// </value>
    /// <remarks>
    /// Don't enable body logging in a production environment, as it might impact
    /// performance and leak sensitive data.
    /// If you need to log body in production, go through compliance and security review.
    /// </remarks>
    /// <example>
    /// A typical set of known text content-types like json, xml or text would be:
    /// <code>
    /// - "application/*+json",
    /// - "application/*+xml",
    /// - "application/json",
    /// - "application/xml",
    /// - "text/*"
    /// </code>
    /// </example>
    public MediaTypeOptions MediaTypeOptions { get; }

    /// <summary>
    /// Gets or sets the maximum request body size to log (in bytes). Defaults to 32 KB.
    /// </summary>
    public int RequestBodyLogLimit { get; set; } = 32 * 1024;

    /// <summary>
    /// Gets or sets the maximum response body size to log (in bytes). Defaults to 32 KB.
    /// </summary>
    public int ResponseBodyLogLimit { get; set; } = 32 * 1024;

    /// <summary>
    /// Gets or sets a value indicating whether the middleware will combine the request, request body, response, response body,
    /// and duration logs into a single log entry. The default is <see langword="true"/>.
    /// </summary>
    public bool CombineLogs { get; set; } = true;
}

#endif
