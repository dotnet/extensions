// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Constants used for incoming HTTP request logging tags.
/// </summary>
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
    public const string RequestHeaderPrefix = "RequestHeader.";

    /// <summary>
    /// HTTP Response Headers prefix.
    /// </summary>
    public const string ResponseHeaderPrefix = "ResponseHeader.";

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
    /// <returns>A read-only <see cref="T:System.Collections.Generic.IReadOnlyList`1" /> of all dimension names.</returns>
    public static IReadOnlyList<string> DimensionNames { get; }
}
