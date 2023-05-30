// Assembly 'Microsoft.Extensions.Telemetry'

namespace Microsoft.Extensions.Telemetry.Tracing;

/// <summary>
/// Sampler type.
/// </summary>
public enum SamplerType
{
    /// <summary>
    /// Always samples traces.
    /// </summary>
    AlwaysOn = 0,
    /// <summary>
    /// Never samples traces.
    /// </summary>
    AlwaysOff = 1,
    /// <summary>
    /// Samples traces according to the specified probability.
    /// </summary>
    TraceIdRatioBased = 2,
    /// <summary>
    /// Samples traces if the parent Activity is sampled.
    /// </summary>
    ParentBased = 3
}
