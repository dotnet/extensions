// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Logging;

/// <summary>
/// Constants used for HTTP client logging tags.
/// </summary>
// There is no OTel semantic convention for HTTP logs, therefore we are using semantic conventions for HTTP spans:
// https://github.com/open-telemetry/semantic-conventions/blob/main/docs/http/http-spans.md.
// "Duration", "RequestBody" and "ResponseBody" are not part of the semantic conventions though.
public static class HttpClientLoggingTagNames
{
    /// <summary>
    /// HTTP Request duration.
    /// </summary>
    public const string Duration = "Duration";

    /// <summary>
    /// HTTP Host.
    /// </summary>
    public const string Host = "server.address";

    /// <summary>
    /// HTTP Method.
    /// </summary>
    public const string Method = "http.request.method";

    /// <summary>
    /// HTTP Path.
    /// </summary>
    public const string Path = "url.path";

    /// <summary>
    /// HTTP Request Body.
    /// </summary>
    public const string RequestBody = "RequestBody";

    /// <summary>
    /// HTTP Response Body.
    /// </summary>
    public const string ResponseBody = "ResponseBody";

    /// <summary>
    /// HTTP Request Headers prefix.
    /// </summary>
    public const string RequestHeaderPrefix = "http.request.header.";

    /// <summary>
    /// HTTP Response Headers prefix.
    /// </summary>
    public const string ResponseHeaderPrefix = "http.response.header.";

    /// <summary>
    /// HTTP Status Code.
    /// </summary>
    public const string StatusCode = "http.response.status_code";

    /// <summary>
    /// Gets a list of all tag names.
    /// </summary>
    /// <returns>A read-only <see cref="IReadOnlyList{String}"/> of all tag names.</returns>
    public static IReadOnlyList<string> TagNames { get; } =
        Array.AsReadOnly(new[]
        {
            Duration,
            Host,
            Method,
            Path,
            RequestBody,
            RequestHeaderPrefix,
            ResponseBody,
            ResponseHeaderPrefix,
            StatusCode
        });
}
