// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents a collection of <see cref="T:Microsoft.Extensions.Http.Resilience.Endpoint" /> with a weight assigned.
/// </summary>
public class WeightedEndpointGroup : EndpointGroup
{
    /// <summary>
    /// Gets or sets the weight of the group.
    /// </summary>
    /// <remarks>
    /// Default value is 32000.
    /// </remarks>
    [Range(1, 64000)]
    public int Weight { get; set; }

    public WeightedEndpointGroup();
}
