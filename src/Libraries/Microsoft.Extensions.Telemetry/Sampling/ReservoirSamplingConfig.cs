// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Configuration for the adaptive (CCKR) log reservoir sampler wired into the logging pipeline.
/// </summary>
public sealed class ReservoirSamplingConfig
{
    /// <summary>
    /// Gets or sets the per-period reservoir capacity (<c>T</c>).
    /// </summary>
    public int Capacity { get; set; } = 128;

    /// <summary>
    /// Gets or sets the per-period novelty-preserve capacity (<c>R</c>). <c>0</c> disables the preserve.
    /// </summary>
    public int PreserveCapacity { get; set; } = 128;

    /// <summary>
    /// Gets or sets the minimum prior-period arrival count below which the frozen frequency table is
    /// discarded and the next period is treated as warmup.
    /// </summary>
    public long MinPeriodCount { get; set; } = 32;

    /// <summary>
    /// Gets or sets the period length. When this much time has elapsed the reservoir is flushed and a
    /// new period begins.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the strategy used to weight callsites unseen in the frozen table.
    /// </summary>
    public UnseenWeightMode UnseenWeightMode { get; set; } = UnseenWeightMode.Chao1;
}
