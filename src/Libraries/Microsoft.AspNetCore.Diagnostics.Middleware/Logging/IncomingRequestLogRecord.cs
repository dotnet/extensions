// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class IncomingRequestLogRecord
{
    /// <summary>
    /// Gets or sets a request host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a request method.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a request path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets request path parameters.
    /// </summary>
    public HttpRouteParameter[]? PathParameters { get; set; }

    /// <summary>
    /// Gets or sets request path parameters count for <see cref="PathParameters"/>.
    /// </summary>
    public int PathParametersCount { get; set; }

    /// <summary>
    /// Gets or sets a request's duration in milliseconds.
    /// </summary>
    public long? Duration { get; set; }

    /// <summary>
    /// Gets or sets response status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets a list of request headers.
    /// </summary>
    public List<KeyValuePair<string, string>>? RequestHeaders { get; set; }

    /// <summary>
    /// Gets or sets a list of response headers.
    /// </summary>
    public List<KeyValuePair<string, string>>? ResponseHeaders { get; set; }

    /// <summary>
    /// Gets or sets enrichment properties.
    /// </summary>
    public LogMethodHelper? EnrichmentPropertyBag { get; set; }

    /// <summary>
    /// Gets or sets parsed request body.
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets parsed response body.
    /// </summary>
    public string? ResponseBody { get; set; }
}
