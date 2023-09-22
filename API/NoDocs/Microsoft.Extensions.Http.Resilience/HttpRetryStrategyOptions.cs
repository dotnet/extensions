// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.Retry;

namespace Microsoft.Extensions.Http.Resilience;

public class HttpRetryStrategyOptions : RetryStrategyOptions<HttpResponseMessage>
{
    public bool ShouldRetryAfterHeader { get; set; }
    public HttpRetryStrategyOptions();
}
