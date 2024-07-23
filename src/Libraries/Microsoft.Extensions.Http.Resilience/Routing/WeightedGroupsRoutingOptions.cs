// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public WeightedGroupSelectionMode SelectionMode { get; set; } = WeightedGroupSelectionMode.EveryAttempt;

    /// <summary>
    /// Gets or sets the collection of weighted endpoints groups.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    [Required]
#if NET8_0_OR_GREATER
    [System.ComponentModel.DataAnnotations.Length(1, int.MaxValue)]
#else
    [Microsoft.Shared.Data.Validation.Length(1)]
#endif
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "TODO")]
    [ValidateEnumeratedItems]
    public IList<WeightedUriEndpointGroup> Groups { get; set; } = new List<WeightedUriEndpointGroup>();
#pragma warning restore CA2227 // Collection properties should be read only
}
