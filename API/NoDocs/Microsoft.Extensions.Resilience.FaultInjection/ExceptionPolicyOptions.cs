// Assembly 'Microsoft.Extensions.Resilience'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class ExceptionPolicyOptions : ChaosPolicyOptionsBase
{
    public string ExceptionKey { get; set; }
    public ExceptionPolicyOptions();
}
