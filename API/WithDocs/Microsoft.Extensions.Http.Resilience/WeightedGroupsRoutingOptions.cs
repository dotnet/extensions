// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents the options for collection of endpoint groups that have a weight assigned.
/// </summary>
/// <remarks>
/// This strategy picks the first endpoint group based on its weight and then selects the remaining groups in order,
/// starting from the first one and omitting the one that was already selected.
/// </remarks>
public class WeightedGroupsRoutingOptions
{
    /// <summary>
    /// Gets or sets the selection mode that determines the behavior of underlying routing strategy.
    /// </summary>
    public WeightedGroupSelectionMode SelectionMode { get; set; }

    /// <summary>
    /// Gets or sets the collection of weighted endpoints groups.
    /// </summary>
    [Required]
    [Length(1, int.MaxValue)]
    [ValidateEnumeratedItems]
    public IList<WeightedUriEndpointGroup> Groups { get; set; }

    public WeightedGroupsRoutingOptions();
}
