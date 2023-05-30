// Assembly 'Microsoft.Extensions.Http.Resilience'

using Polly.RateLimiting;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="T:Polly.RateLimiting.RateLimiterStrategyOptions" /> for HTTP scenarios.
/// </summary>
public class HttpRateLimiterStrategyOptions : RateLimiterStrategyOptions
{
    public HttpRateLimiterStrategyOptions();
}
