// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Registered names for <see cref="T:Microsoft.Extensions.Telemetry.Latency.ILatencyContext" />.
/// </summary>
public class LatencyContextRegistrationOptions
{
    /// <summary>
    /// Gets or sets the list of registered checkpoint names.
    /// </summary>
    [Required]
    public IReadOnlyList<string> CheckpointNames { get; set; }

    /// <summary>
    /// Gets or sets the list of registered measure names.
    /// </summary>
    [Required]
    public IReadOnlyList<string> MeasureNames { get; set; }

    /// <summary>
    /// Gets or sets the list of registered tag names.
    /// </summary>
    [Required]
    public IReadOnlyList<string> TagNames { get; set; }

    public LatencyContextRegistrationOptions();
}
