// Assembly 'Microsoft.Extensions.Resilience'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class FaultInjectionOptions
{
    public IDictionary<string, ChaosPolicyOptionsGroup> ChaosPolicyOptionsGroups { get; set; }
    public FaultInjectionOptions();
}
