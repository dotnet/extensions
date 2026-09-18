// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Provides configuration for the adaptive CCKR log sampler.
/// </summary>
public sealed class ReservoirSamplingConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether CCKR sampling is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the per-period reservoir capacity (<c>T</c>).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 128;

    /// <summary>
    /// Gets or sets the per-period novelty-preserve capacity (<c>R</c>). <c>0</c> disables the preserve.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PreserveCapacity { get; set; } = 128;

    /// <summary>
    /// Gets or sets the minimum prior-period arrival count below which the frozen frequency table is
    /// discarded and the next period is treated as warmup.
    /// </summary>
    [Range(0, long.MaxValue)]
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

    /// <summary>
    /// Gets or sets the log levels that bypass CCKR and are emitted normally.
    /// </summary>
    [Required]
    public IList<LogLevel> RetainAllLogLevels { get; set; } = [LogLevel.Error, LogLevel.Critical];

    /// <summary>
    /// Gets or sets category patterns that bypass CCKR and are emitted normally.
    /// </summary>
    /// <remarks>
    /// Matching is case-insensitive. A pattern can contain one <c>*</c> wildcard.
    /// </remarks>
    [Required]
    public IList<string> RetainAllCategories { get; set; } = [];

    /// <summary>
    /// Gets or sets category patterns eligible for CCKR sampling.
    /// </summary>
    /// <remarks>
    /// An empty collection applies CCKR to every category not covered by a retain-all policy.
    /// Matching is case-insensitive. A pattern can contain one <c>*</c> wildcard.
    /// </remarks>
    [Required]
    public IList<string> SampledCategories { get; set; } = [];

    /// <summary>
    /// Gets or sets event identifiers that bypass CCKR and are emitted normally.
    /// </summary>
    [Required]
    public IList<int> RetainAllEventIds { get; set; } = [];
}
