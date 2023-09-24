// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using Microsoft.Extensions.Http.Diagnostics;

namespace System.Net.Http;

public static class HttpDiagnosticsHttpRequestMessageExtensions
{
    public static void SetRequestMetadata(this HttpRequestMessage request, RequestMetadata metadata);
    public static RequestMetadata? GetRequestMetadata(this HttpRequestMessage request);
}
