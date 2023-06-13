// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

#pragma warning disable CA2227 // Collection properties should be read only

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

/// <summary>
/// Options for the fake logger.
/// </summary>
public class FakeLogCollectorOptions
{
    /// <summary>
    /// Gets or sets the logger categories for whom records are collected.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty set, which doesn't filter any records.
    /// If not empty, only records coming from loggers in these categories will be collected by the fake logger.
    /// </remarks>
    public ISet<string> FilteredCategories { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the logging levels for whom records are collected.
    /// </summary>
    /// <remarks>
    /// Defaults to an empty set, which doesn't filter any records.
    /// If not empty, only records with the given level will be collected by the fake logger.
    /// </remarks>
    public ISet<LogLevel> FilteredLevels { get; set; } = new HashSet<LogLevel>();

    /// <summary>
    /// Gets or sets a value indicating whether to collect records that are logged when the associated log level is currently disabled.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool CollectRecordsForDisabledLogLevels { get; set; } = true;

    /// <summary>
    /// Gets or sets the time provider to use when time-stamping log records.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="TimeProvider.System"/>.
    /// </remarks>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Gets or sets an output sink where every record harvested by the collector is sent.
    /// </summary>
    /// <remarks>
    /// By setting this property, you can have all log records harvested by the collector be copied somewhere convenient.
    /// Defaults to <see langword="null"/>.
    /// </remarks>
    public Action<string>? OutputSink { get; set; }

    /// <summary>
    /// Gets or sets a delegate that is used to format log records in a specialized way before sending them to the registered output sink.
    /// </summary>
    public Func<FakeLogRecord, string> OutputFormatter { get; set; } = FakeLogRecord.Formatter;
}
