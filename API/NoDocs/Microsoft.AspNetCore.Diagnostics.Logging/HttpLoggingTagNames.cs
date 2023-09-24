// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

public static class HttpLoggingTagNames
{
    public const string Duration = "duration";
    public const string Host = "httpHost";
    public const string Method = "httpMethod";
    public const string Path = "httpPath";
    public const string RequestHeaderPrefix = "httpRequestHeader_";
    public const string ResponseHeaderPrefix = "httpResponseHeader_";
    public const string RequestBody = "httpRequestBody";
    public const string ResponseBody = "httpResponseBody";
    public const string StatusCode = "httpStatusCode";
    public static IReadOnlyList<string> DimensionNames { get; }
}
