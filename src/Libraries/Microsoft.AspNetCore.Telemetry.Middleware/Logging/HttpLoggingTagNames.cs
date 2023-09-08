// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Constants used for incoming HTTP request logging tags.
/// </summary>
[Experimental(diagnosticId: Experiments.HttpLogging, UrlFormat = Experiments.UrlFormat)]
public static class HttpLoggingTagNames
{
    /// <summary>
    /// HTTP Request duration in milliseconds.
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
    /// HTTP Request Headers prefix.
    /// </summary>
    public const string RequestHeaderPrefix = "RequestHeader_";

    /// <summary>
    /// HTTP Response Headers prefix.
    /// </summary>
    public const string ResponseHeaderPrefix = "ResponseHeader_";

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
#endif
