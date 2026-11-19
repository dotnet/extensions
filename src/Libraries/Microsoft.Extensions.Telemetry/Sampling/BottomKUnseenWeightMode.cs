// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// Specifies how to weight a log event when no records with the same numeric event ID were
/// observed for its logger category during the previous sampling period.
/// </summary>
[Experimental(DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public enum BottomKUnseenWeightMode
{
    /// <summary>
    /// Estimates the expected frequency of an event that was not observed and uses the inverse of
    /// that frequency as its sampling weight.
    /// </summary>
    Chao1,

    /// <summary>
    /// Uses the same inverse-frequency weight as the least frequent event observed for the logger
    /// category during the previous sampling period.
    /// </summary>
    RarestSeen,
}
#endif
