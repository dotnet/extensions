// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

public class HedgingEndpointOptions
{
    [Required]
    [ValidateObjectMembers]
    public HttpRateLimiterStrategyOptions RateLimiterOptions { get; set; }
    [Required]
    [ValidateObjectMembers]
    public HttpCircuitBreakerStrategyOptions CircuitBreakerOptions { get; set; }
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TimeoutOptions { get; set; }
    public HedgingEndpointOptions();
}
