// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Logging;

/// <summary>
/// Constants used for HTTP client logging tags.
/// </summary>
public static class HttpClientLoggingTagNames
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
    /// Gets a list of all tag names.
    /// </summary>
    /// <returns>A read-only <see cref="T:System.Collections.Generic.IReadOnlyList`1" /> of all tag names.</returns>
    public static IReadOnlyList<string> TagNames { get; }
}
