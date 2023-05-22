// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

/// <summary>
/// Parsed HTTP information.
/// </summary>
internal sealed class LogRecord
{
    /// <summary>
    /// Gets or sets HTTP host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets HTTP request method.
    /// </summary>
    public HttpMethod? Method { get; set; }

    /// <summary>
    /// Gets or sets parsed request path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets HTTP request duration in milliseconds.
    /// </summary>
    public long Duration { get; set; }

    /// <summary>
    /// Gets or sets HTTP response status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets parsed list of request headers.
    /// </summary>
    public List<KeyValuePair<string, string>>? RequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets parsed list of headers.
    /// </summary>
    public List<KeyValuePair<string, string>>? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets or sets parsed request body.
    /// </summary>
    public string RequestBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets parsed response body.
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets enrichment properties.
    /// </summary>
    public LogMethodHelper? EnrichmentProperties { get; set; }
}
