// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for sampling.
/// </summary>
public class SamplingOptions
{
    /// <summary>
    /// Gets or sets the type of the sampler.
    /// </summary>
    /// <value>The default is the <see cref="F:Microsoft.Extensions.Telemetry.Tracing.SamplerType.AlwaysOn" /> sampler.</value>
    public SamplerType SamplerType { get; set; }

    /// <summary>
    /// Gets or sets options for the parent based sampler.
    /// </summary>
    /// <value>The default is <see langword="null" />.</value>
    [ValidateObjectMembers]
    public ParentBasedSamplerOptions? ParentBasedSamplerOptions { get; set; }

    /// <summary>
    /// Gets or sets options for the trace ID ratio-based sampler.
    /// </summary>
    /// <value>The default is <see langword="null" />.</value>
    [ValidateObjectMembers]
    public TraceIdRatioBasedSamplerOptions? TraceIdRatioBasedSamplerOptions { get; set; }

    public SamplingOptions();
}
