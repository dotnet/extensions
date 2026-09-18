// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Strategy for weighting callsites that were not present in the previous period's frozen
/// frequency table.
/// </summary>
public enum UnseenWeightMode
{
    /// <summary>
    /// Chao1 / Good-Turing missing-mass estimate.
    /// </summary>
    Chao1,

    /// <summary>
    /// Rarest-seen rule: an unseen callsite is weighted the same as the rarest callsite already
    /// observed (the inverse of the smallest observed frequency).
    /// </summary>
    RarestSeen,
}
