// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for the trace Id ratio based sampler.
/// </summary>
public class TraceIdRatioBasedSamplerOptions
{
    /// <summary>
    /// Gets or sets the desired probability of sampling.
    /// </summary>
    /// <value>The default is 1.</value>
    [Range(0.0, 1.0)]
    public double Probability { get; set; }

    public TraceIdRatioBasedSamplerOptions();
}
