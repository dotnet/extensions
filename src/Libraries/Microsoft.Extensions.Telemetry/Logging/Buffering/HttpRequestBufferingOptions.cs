// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// The options for LoggerBuffer.
/// </summary>
public class HttpRequestBufferingOptions
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
    /// Gets or sets the size of the buffer for a request.
    /// </summary>
    public int PerRequestCapacity { get; set; } = 1_000;

    /// <summary>
    /// Gets or sets the size of the buffer for a request.
    /// </summary>
    public int GlobalCapacity { get; set; } = 1_000_000;

    /// <summary>
    /// Gets or sets the filter delegate to determine what to buffer.
    /// </summary>
    internal Func<string?, EventId?, LogLevel?, bool> Filter { get; set; } = (_, _, _) => false;
}
