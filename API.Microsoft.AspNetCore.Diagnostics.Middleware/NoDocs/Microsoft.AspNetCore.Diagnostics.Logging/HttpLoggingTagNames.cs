// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

public static class HttpLoggingTagNames
{
    public const string Duration = "Duration";
    public const string Host = "Host";
    public const string Method = "Method";
    public const string Path = "Path";
    public const string RequestHeaderPrefix = "RequestHeader.";
    public const string ResponseHeaderPrefix = "ResponseHeader.";
    public const string RequestBody = "RequestBody";
    public const string ResponseBody = "ResponseBody";
    public const string StatusCode = "StatusCode";
    public static IReadOnlyList<string> DimensionNames { get; }
}
