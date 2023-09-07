// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

public interface IHttpClientChaosPolicyFactory
{
    AsyncInjectOutcomePolicy<HttpResponseMessage> CreateHttpResponsePolicy();
}
