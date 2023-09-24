// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using Microsoft.Extensions.Http.Diagnostics;

namespace System.Net;

public static class HttpDiagnosticsHttpWebRequestExtensions
{
    public static void SetRequestMetadata(this HttpWebRequest request, RequestMetadata metadata);
    public static RequestMetadata? GetRequestMetadata(this HttpWebRequest request);
}
