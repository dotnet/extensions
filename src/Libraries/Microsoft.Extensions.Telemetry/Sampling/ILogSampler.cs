// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// A per-thread adaptive log-event sampler. Callers first ask <see cref="Admit(TCallsite)"/> whether
/// an event should be kept; only for a non-<see cref="AdmissionKind.Skip"/> result do they format the
/// payload and call <see cref="Insert"/>. Periodically the caller drains the current period via
/// <see cref="Flush"/> / <see cref="FlushInto"/>.
/// </summary>
/// <typeparam name="TCallsite">
/// The callsite identifier type. In production this is the durable log identifier: a stable key per
/// logging statement is what makes adaptive, per-callsite sampling possible.
/// </typeparam>
/// <typeparam name="TPayload">The formatted log payload type.</typeparam>
internal interface ILogSampler<TCallsite, TPayload>
    where TCallsite : notnull
{
    /// <summary>
    /// Hot path: decide whether an event for <paramref name="callsite"/> should be kept. When the
    /// result is <see cref="AdmissionKind.Skip"/> the caller must drop the event without formatting
    /// it. Otherwise the caller must format the payload and pass the returned admission verbatim to
    /// <see cref="Insert"/>.
    /// </summary>
    /// <param name="callsite">The callsite identifier (durable ID).</param>
    /// <returns>The admission decision.</returns>
    Admission Admit(TCallsite callsite);

    /// <summary>
    /// Store a formatted payload previously approved by <see cref="Admit(TCallsite)"/>.
    /// </summary>
    /// <param name="callsite">The callsite identifier.</param>
    /// <param name="admission">The admission returned by <see cref="Admit(TCallsite)"/>.</param>
    /// <param name="payload">The formatted payload.</param>
    void Insert(TCallsite callsite, Admission admission, TPayload payload);

    /// <summary>
    /// Drain the current period's sample into a caller-supplied buffer, avoiding the per-flush
    /// allocation of <see cref="Flush"/>. The sum of
    /// <see cref="SampledRecord{TCallsite, TPayload}.SamplingCount"/> is an unbiased Horvitz-Thompson
    /// estimator of the period's total arrival count.
    /// </summary>
    /// <param name="output">The buffer to append records to.</param>
    void FlushInto(ICollection<SampledRecord<TCallsite, TPayload>> output);

    /// <summary>
    /// Drain the current period's sample. Allocates a fresh list each call; use <see cref="FlushInto"/>
    /// to recycle a buffer.
    /// </summary>
    /// <returns>The sampled records for the period.</returns>
    List<SampledRecord<TCallsite, TPayload>> Flush();
}
