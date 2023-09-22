// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class LatencyPolicyOptions : ChaosPolicyOptionsBase
{
    [TimeSpan("00:00:00", "00:10:00")]
    public TimeSpan Latency { get; set; }
    public LatencyPolicyOptions();
}
