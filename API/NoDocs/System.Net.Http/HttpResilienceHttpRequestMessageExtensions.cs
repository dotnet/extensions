// Assembly 'Microsoft.Extensions.Http.Resilience'

using Polly;

namespace System.Net.Http;

public static class HttpResilienceHttpRequestMessageExtensions
{
    public static ResilienceContext? GetResilienceContext(this HttpRequestMessage requestMessage);
    public static void SetResilienceContext(this HttpRequestMessage requestMessage, ResilienceContext? resilienceContext);
}
