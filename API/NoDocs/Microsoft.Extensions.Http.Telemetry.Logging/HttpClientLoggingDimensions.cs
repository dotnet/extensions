// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

public static class HttpClientLoggingDimensions
{
    public const string Duration = "duration";
    public const string Host = "httpHost";
    public const string Method = "httpMethod";
    public const string Path = "httpPath";
    public const string RequestBody = "httpRequestBody";
    public const string ResponseBody = "httpResponseBody";
    public const string RequestHeaderPrefix = "httpRequestHeader_";
    public const string ResponseHeaderPrefix = "httpResponseHeader_";
    public const string StatusCode = "httpStatusCode";
    public static IReadOnlyList<string> DimensionNames { get; }
}
