// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Provides configuration for adaptive bottom-K log sampling.
/// </summary>
[Experimental(DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class BottomKLogSamplingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether bottom-K sampling is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the per-period reservoir capacity (<c>T</c>).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; } = 128;

    /// <summary>
    /// Gets or sets the period length. When this much time has elapsed the reservoir is flushed and a
    /// new period begins.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets how sampling weights a log event when no records with the same numeric event ID
    /// were observed for its logger category during the previous sampling period.
    /// </summary>
    public BottomKUnseenWeightMode UnseenWeightMode { get; set; } = BottomKUnseenWeightMode.Chao1;

    /// <summary>
    /// Gets or sets the rules used to determine which log messages are processed by bottom-K sampling.
    /// </summary>
    /// <remarks>
    /// The most specific matching rule is applied. If no rule matches, the log message is emitted normally.
    /// An empty collection applies bottom-K sampling to every log message.
    /// </remarks>
    [Required]
    public IList<BottomKLogSamplingFilterRule> Rules { get; set; } = [];
}
#endif
