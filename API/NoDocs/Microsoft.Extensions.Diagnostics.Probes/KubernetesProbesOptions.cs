// Assembly 'Microsoft.Extensions.Diagnostics.Probes'

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.Probes;

public class KubernetesProbesOptions
{
    public class EndpointOptions
    {
        [Range(1, 65535)]
        public int TcpPort { get; set; }
        [Range(1, 10000)]
        public int MaxPendingConnections { get; set; }
        [TimeSpan("00:00:05", "00:05:00")]
        public TimeSpan HealthAssessmentPeriod { get; set; }
        public Func<HealthCheckRegistration, bool>? FilterChecks { get; set; }
        public EndpointOptions();
    }
    public EndpointOptions LivenessProbe { get; set; }
    public EndpointOptions StartupProbe { get; set; }
    public EndpointOptions ReadinessProbe { get; set; }
    public KubernetesProbesOptions();
}
