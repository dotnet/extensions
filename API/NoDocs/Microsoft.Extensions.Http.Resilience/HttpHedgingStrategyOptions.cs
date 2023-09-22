// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.Hedging;

namespace Microsoft.Extensions.Http.Resilience;

public class HttpHedgingStrategyOptions : HedgingStrategyOptions<HttpResponseMessage>
{
    public HttpHedgingStrategyOptions();
}
