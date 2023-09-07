// Assembly 'Microsoft.Extensions.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class HttpResponseInjectionPolicyOptions : ChaosPolicyOptionsBase
{
    [EnumDataType(typeof(HttpStatusCode))]
    public HttpStatusCode StatusCode { get; set; }
    public string? HttpContentKey { get; set; }
    public HttpResponseInjectionPolicyOptions();
}
