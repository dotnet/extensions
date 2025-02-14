// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// The options for global log buffering.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class GlobalLogBufferingOptions
{
    /// <summary>
    /// Gets or sets the time to suspend the buffering after flushing.
    /// </summary>
    /// <remarks>
    /// Use this to temporarily suspend buffering after a flush, e.g. in case of an incident you may want all logs to be emitted immediately,
    /// so the buffering will be suspended for the <see paramref="SuspendAfterFlushDuration"/> time.
    /// </remarks>
    public TimeSpan SuspendAfterFlushDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maxiumum size of each individual log record in bytes. If the size of a log record exceeds this limit, it won't be buffered.
    /// </summary>
    /// TO DO: add validation.
    public int MaxLogRecordSizeInBytes { get; set; } = 50_000;

    /// <summary>
    /// Gets or sets the maximum size of the buffer in bytes. If adding a new log entry would cause the buffer size to exceed this limit,
    /// the oldest buffered log records will be dropped to make room.
    /// </summary>
    /// TO DO: add validation.
    public int MaxBufferSizeInBytes { get; set; } = 500_000_000;

#pragma warning disable CA2227 // Collection properties should be read only - setter is necessary for options pattern
    /// <summary>
    /// Gets or sets the collection of <see cref="LogBufferingFilterRule"/> used for filtering log messages for the purpose of further buffering.
    /// </summary>
    public IList<LogBufferingFilterRule> Rules { get; set; } = [];
#pragma warning restore CA2227
}
