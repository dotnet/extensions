﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// The options for global log buffering.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class GlobalLogBufferingOptions
{
    private const int DefaultMaxBufferSizeInBytes = 500 * 1024 * 1024; // 500 MB.
    private const int DefaultMaxLogRecordSizeInBytes = 50 * 1024; // 50 KB.

    private const int MinimumAutoFlushDuration = 0;
    private const int MaximumAutoFlushDuration = 1000 * 60 * 60 * 24; // 1 day.

    private const long MinimumBufferSizeInBytes = 1;
    private const long MaximumBufferSizeInBytes = 10L * 1024 * 1024 * 1024; // 10 GB.

    private const long MinimumLogRecordSizeInBytes = 1;
    private const long MaximumLogRecordSizeInBytes = 10 * 1024 * 1024; // 10 MB.

    private static readonly TimeSpan _defaultAutoFlushDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the time to do automatic flushing after manual flushing was triggered.
    /// </summary>
    /// <remarks>
    /// Use this to temporarily suspend buffering after a flush, e.g. in case of an incident you may want all logs to be emitted immediately,
    /// so the buffering will be suspended for the <see paramref="AutoFlushDuration"/> time.
    /// </remarks>
    [TimeSpan(MinimumAutoFlushDuration, MaximumAutoFlushDuration)]
    public TimeSpan AutoFlushDuration { get; set; } = _defaultAutoFlushDuration;

    /// <summary>
    /// Gets or sets the maximum size of each individual log record in bytes.
    /// </summary>
    /// <remarks>
    /// If the size of a log record exceeds this limit, it won't be buffered.
    /// </remarks>
    [Range(MinimumLogRecordSizeInBytes, MaximumLogRecordSizeInBytes)]
    public int MaxLogRecordSizeInBytes { get; set; } = DefaultMaxLogRecordSizeInBytes;

    /// <summary>
    /// Gets or sets the maximum size of the buffer in bytes.
    /// </summary>
    /// <remarks>
    /// If adding a new log entry would cause the buffer size to exceed this limit,
    /// the oldest buffered log records will be dropped to make room.
    /// </remarks>
    [Range(MinimumBufferSizeInBytes, MaximumBufferSizeInBytes)]
    public int MaxBufferSizeInBytes { get; set; } = DefaultMaxBufferSizeInBytes;

#pragma warning disable CA2227 // Collection properties should be read only - setter is necessary for options pattern.
    /// <summary>
    /// Gets or sets the collection of <see cref="LogBufferingFilterRule"/> used for filtering log messages for the purpose of further buffering.
    /// </summary>
    /// <remarks>
    /// If a log entry matches a rule, it will be buffered. Consequently, it will later be emitted when the buffer is flushed.
    /// If a log entry does not match any rule, it will be emitted normally.
    /// If the buffer size limit is reached, the oldest buffered log entries will be dropped (not emitted!) to make room for new ones.
    /// If a log entry size is greater than <see cref="MaxLogRecordSizeInBytes"/>, it will not be buffered and will be emitted normally.
    /// </remarks>
    [Required]
    public IList<LogBufferingFilterRule> Rules { get; set; } = [];
#pragma warning restore CA2227
}

#endif
