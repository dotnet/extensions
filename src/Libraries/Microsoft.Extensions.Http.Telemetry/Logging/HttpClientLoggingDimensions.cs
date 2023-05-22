// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Constants used for HTTP client logging dimensions.
/// </summary>
public static class HttpClientLoggingDimensions
{
    /// <summary>
    /// HTTP Request duration.
    /// </summary>
    public const string Duration = "duration";

    /// <summary>
    /// HTTP Host.
    /// </summary>
    public const string Host = "httpHost";

    /// <summary>
    /// HTTP Method.
    /// </summary>
    public const string Method = "httpMethod";

    /// <summary>
    /// HTTP Path.
    /// </summary>
    public const string Path = "httpPath";

    /// <summary>
    /// HTTP Request Body.
    /// </summary>
    public const string RequestBody = "httpRequestBody";

    /// <summary>
    /// HTTP Response Body.
    /// </summary>
    public const string ResponseBody = "httpResponseBody";

    /// <summary>
    /// HTTP Request Headers prefix.
    /// </summary>
    public const string RequestHeaderPrefix = "httpRequestHeader_";

    /// <summary>
    /// HTTP Response Headers prefix.
    /// </summary>
    public const string ResponseHeaderPrefix = "httpResponseHeader_";

    /// <summary>
    /// HTTP Status Code.
    /// </summary>
    public const string StatusCode = "httpStatusCode";

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
            RequestBody,
            RequestHeaderPrefix,
            ResponseBody,
            ResponseHeaderPrefix,
            StatusCode
        });
}
