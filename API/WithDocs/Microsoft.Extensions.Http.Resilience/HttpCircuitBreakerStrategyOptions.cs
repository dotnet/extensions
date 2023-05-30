// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.CircuitBreaker;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="T:Polly.CircuitBreaker.CircuitBreakerStrategyOptions`1" /> for <see cref="T:System.Net.Http.HttpResponseMessage" /> results.
/// </summary>
public class HttpCircuitBreakerStrategyOptions : AdvancedCircuitBreakerStrategyOptions<HttpResponseMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpCircuitBreakerStrategyOptions" /> class.
    /// </summary>
    /// <remarks>
    /// By default the options is set to handle only transient failures,
    /// i.e. timeouts, 5xx responses and <see cref="T:System.Net.Http.HttpRequestException" /> exceptions.
    /// </remarks>
    public HttpCircuitBreakerStrategyOptions();
}
