// Assembly 'Microsoft.Extensions.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class ChaosPolicyOptionsBase
{
    public bool Enabled { get; set; }
    [Range(0, 1)]
    public double FaultInjectionRate { get; set; }
    protected ChaosPolicyOptionsBase();
}
