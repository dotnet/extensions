// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents the selection mode used in <see cref="WeightedGroupsRoutingOptions"/>.
/// </summary>
public enum WeightedGroupSelectionMode
{
    /// <summary>
    /// In this selection mode the weight is used for every pick of <see cref="WeightedEndpointGroup"/>.
    /// </summary>
    EveryAttempt,

    /// <summary>
    /// In this selection mode the weight is only used to pick initial <see cref="WeightedEndpointGroup"/>.
    /// Remaining groups are picked in order, starting from the first, finishing with last and skipping already picked group.
    /// </summary>
    InitialAttempt
}
