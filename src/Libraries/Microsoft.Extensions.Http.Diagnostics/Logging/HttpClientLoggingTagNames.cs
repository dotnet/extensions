// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Logging;

/// <summary>
/// Constants used for HTTP client logging tags.
/// </summary>
public static class HttpClientLoggingTagNames
{
    /// <summary>
    /// HTTP Request duration.
    /// </summary>
    public const string Duration = "Duration";

    /// <summary>
    /// HTTP Host.
    /// </summary>
    public const string Host = "Host";

    /// <summary>
    /// HTTP Method.
    /// </summary>
    public const string Method = "Method";

    /// <summary>
    /// HTTP Path.
    /// </summary>
    public const string Path = "Path";

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
    public const string RequestHeaderPrefix = "RequestHeader.";

    /// <summary>
    /// HTTP Response Headers prefix.
    /// </summary>
    public const string ResponseHeaderPrefix = "ResponseHeader.";

    /// <summary>
    /// HTTP Status Code.
    /// </summary>
    public const string StatusCode = "StatusCode";

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
