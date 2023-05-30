// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.CircuitBreaker;

namespace Microsoft.Extensions.Http.Resilience;

public class HttpCircuitBreakerStrategyOptions : AdvancedCircuitBreakerStrategyOptions<HttpResponseMessage>
{
    public HttpCircuitBreakerStrategyOptions();
}
