// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

public class HttpStandardHedgingResilienceOptions
{
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TotalRequestTimeoutOptions { get; set; }
    [Required]
    [ValidateObjectMembers]
    public HttpHedgingStrategyOptions HedgingOptions { get; set; }
    [Required]
    [ValidateObjectMembers]
    public HedgingEndpointOptions EndpointOptions { get; set; }
    public HttpStandardHedgingResilienceOptions();
}
