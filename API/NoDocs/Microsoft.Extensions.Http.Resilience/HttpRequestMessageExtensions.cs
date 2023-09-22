// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

public static class HttpRequestMessageExtensions
{
    public static ResilienceContext? GetResilienceContext(this HttpRequestMessage requestMessage);
    public static void SetResilienceContext(this HttpRequestMessage requestMessage, ResilienceContext? resilienceContext);
}
