// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Factory for HTTP response chaos policy creation.
/// </summary>
public interface IHttpClientChaosPolicyFactory
{
    /// <summary>
    /// Creates an async HTTP response fault injection policy with delegate functions
    /// to fetch fault injection settings from <see cref="T:Polly.Context" />.
    /// </summary>
    /// <returns>
    /// An HTTP response fault injection policy.
    /// </returns>
    AsyncInjectOutcomePolicy<HttpResponseMessage> CreateHttpResponsePolicy();
}
