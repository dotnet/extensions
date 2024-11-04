// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// The options for ratio based sampling.
/// </summary>
public class RatioBasedSamplerOptions
{
    /// <summary>
    /// Gets the collection of <see cref="RatioBasedSamplerFilterRule"/> used for filtering log messages.
    /// </summary>
    public IList<RatioBasedSamplerFilterRule> Rules => RulesInternal;

    // Concrete representation of the rule list
    internal List<RatioBasedSamplerFilterRule> RulesInternal { get; } = [];
}
