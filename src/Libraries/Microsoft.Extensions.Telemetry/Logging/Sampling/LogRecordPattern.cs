// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// A pattern to match log records against.
/// </summary>
public class LogRecordPattern
{
    /// <summary>
    /// Gets or sets log record category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets log record event ID.
    /// </summary>
    public EventId? EventId { get; set; }

    /// <summary>
    /// Gets or sets log record log level.
    /// </summary>
    public LogLevel? LogLevel { get; set; }

    /// <summary>
    /// Gets or sets log record state tags.
    /// </summary>
    public KeyValuePair<string, string>[]? Tags { get; set; }

    /// <summary>
    /// Gets or sets log record timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
