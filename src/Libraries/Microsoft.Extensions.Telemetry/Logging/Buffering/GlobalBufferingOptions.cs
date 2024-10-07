// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// The options for LoggerBuffer.
/// </summary>
public class GlobalBufferingOptions
{
    /// <summary>
    /// Gets or sets the time to suspend the buffer after flushing.
    /// </summary>
    /// <remarks>
    /// Use this to temporarily suspend buffering after a flush, e.g. in case of an incident you may want all logs to be emitted immediately,
    /// so the buffering will be suspended for the <see paramref="SuspendAfterFlushDuration"/> time.
    /// </remarks>
    public TimeSpan SuspendAfterFlushDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the duration to keep logs in the buffer.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the size of the buffer.
    /// </summary>
    public int Capacity { get; set; } = 1_000_000;

    /// <summary>
    /// Gets the collection of <see cref="LoggerFilterRule"/> used for filtering log messages.
    /// </summary>
    public IList<LoggerFilterRule> Rules => RulesInternal;

    // Concrete representation of the rule list
    internal List<LoggerFilterRule> RulesInternal { get; } = new List<LoggerFilterRule>();
}
