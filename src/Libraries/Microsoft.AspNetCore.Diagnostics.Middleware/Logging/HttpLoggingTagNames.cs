// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Constants used for incoming HTTP request logging tags.
/// </summary>
// There is no OTel semantic convention for HTTP logs, therefore we are using
// semantic conventions for HTTP spans:
// https://github.com/open-telemetry/semantic-conventions/blob/main/docs/http/http-spans.md.
// Some dimensions (e.g. Host and Method) were not renamed, though they had corresponding
// names in the semantic conventions. The reason is that they are coming from ASP.NET Core 8
// HttpLogging component, therefore we cannot rename them.
public static class HttpLoggingTagNames
{
    /// <summary>
    /// HTTP Request duration in milliseconds.
    /// </summary>
    public const string Duration = "Duration";

    /// <summary>
    /// HTTP Host.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/http/http-spans.md#http-server-semantic-conventions.
    public const string Host = "server.address";

    /// <summary>
    /// HTTP Method.
    /// </summary>
    public const string Method = "Method";

    /// <summary>
    /// HTTP Path.
    /// </summary>
    public const string Path = "Path";

    /// <summary>
    /// HTTP Request Headers prefix.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/http/http-spans.md#common-attributes.
    public const string RequestHeaderPrefix = "http.request.header.";

    /// <summary>
    /// HTTP Response Headers prefix.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/http/http-spans.md#common-attributes.
    public const string ResponseHeaderPrefix = "http.response.header.";

    /// <summary>
    /// HTTP Request Body.
    /// </summary>
    public const string RequestBody = "RequestBody";

    /// <summary>
    /// HTTP Response Body.
    /// </summary>
    public const string ResponseBody = "ResponseBody";

    /// <summary>
    /// HTTP Status Code.
    /// </summary>
    public const string StatusCode = "StatusCode";

    /// <summary>
    /// Gets a list of all dimension names.
    /// </summary>
    /// <returns>A read-only <see cref="IReadOnlyList{String}"/> of all dimension names.</returns>
    public static IReadOnlyList<string> DimensionNames { get; } =
        Array.AsReadOnly(new[]
        {
            Duration,
            Host,
            Method,
            Path,
            RequestHeaderPrefix,
            ResponseHeaderPrefix,
            RequestBody,
            ResponseBody,
            StatusCode
        });
}
