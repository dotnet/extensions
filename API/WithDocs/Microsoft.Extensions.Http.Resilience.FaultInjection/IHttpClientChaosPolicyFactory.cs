// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Factory for http response chaos policy creation.
/// </summary>
public interface IHttpClientChaosPolicyFactory
{
    /// <summary>
    /// Creates an async http response fault injection policy with delegate functions
    /// to fetch fault injection settings from <see cref="T:Polly.Context" />.
    /// </summary>
    /// <returns>
    /// An http response fault injection policy,
    /// an instance of <see cref="T:Polly.Contrib.Simmy.Outcomes.AsyncInjectOutcomePolicy`1" />.
    /// </returns>
    AsyncInjectOutcomePolicy<HttpResponseMessage> CreateHttpResponsePolicy();
}
