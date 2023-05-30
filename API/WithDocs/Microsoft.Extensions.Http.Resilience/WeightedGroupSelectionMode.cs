// Assembly 'Microsoft.Extensions.Http.Resilience'

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents the selection mode used in <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedGroupsRoutingOptions" />.
/// </summary>
public enum WeightedGroupSelectionMode
{
    /// <summary>
    /// In this selection mode the weight is used for every pick of <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedEndpointGroup" />.
    /// </summary>
    EveryAttempt = 0,
    /// <summary>
    /// In this selection mode the weight is only used to pick initial <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedEndpointGroup" />.
    /// Remaining groups are picked in order, starting from the first, finishing with last and skipping already picked group.
    /// </summary>
    InitialAttempt = 1
}
