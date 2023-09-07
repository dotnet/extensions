// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Options for sampling.
/// </summary>
public class SamplingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use parent-based sampling.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false" />.
    /// </remarks>
    public bool ParentBased { get; set; }

    /// <summary>
    /// Gets or sets the type of sampler to be used for making sampling decision for root activity.
    /// </summary>
    /// <remarks>
    /// Defaults to the <see cref="F:Microsoft.Extensions.Telemetry.Tracing.SamplerType.AlwaysOn" /> sampler.
    /// </remarks>
    [EnumDataType(typeof(SamplerType))]
    public SamplerType SamplerType { get; set; }

    /// <summary>
    /// Gets or sets the desired probability of sampling when using <see cref="F:Microsoft.Extensions.Telemetry.Tracing.SamplerType.RatioBased" /> samplers.
    /// </summary>
    /// <remarks>
    /// Valid values are in the range from 0 to 1, inclusive. Defaults to 1.
    /// </remarks>
    [Range(0.0, 1.0)]
    public double SampleRate { get; set; }

    public SamplingOptions();
}
