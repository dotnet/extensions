// Assembly 'Microsoft.Extensions.Http.Resilience'

using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="T:Polly.Timeout.TimeoutStrategyOptions" /> for HTTP scenarios.
/// </summary>
public class HttpTimeoutStrategyOptions : TimeoutStrategyOptions
{
    public HttpTimeoutStrategyOptions();
}
