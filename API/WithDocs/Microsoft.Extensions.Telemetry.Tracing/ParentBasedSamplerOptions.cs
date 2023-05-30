// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for the parent based sampler.
/// </summary>
public class ParentBasedSamplerOptions
{
    /// <summary>
    /// Gets or sets the type of sampler to be used for making sampling decision for root activity.
    /// </summary>
    /// <value>The default is the <see cref="F:Microsoft.Extensions.Telemetry.Tracing.SamplerType.AlwaysOn" /> sampler.</value>
    public SamplerType RootSamplerType { get; set; }

    /// <summary>
    /// Gets or sets options for the trace Id ratio based sampler.
    /// </summary>
    /// <value>The default is <see langword="null" />.</value>
    [ValidateObjectMembers]
    public TraceIdRatioBasedSamplerOptions? TraceIdRatioBasedSamplerOptions { get; set; }

    public ParentBasedSamplerOptions();
}
