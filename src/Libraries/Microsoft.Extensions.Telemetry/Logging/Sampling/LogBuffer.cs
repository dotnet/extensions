// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Represents a circular log buffer configuration.
/// </summary>
public class LogBuffer
{
    /// <summary>
    /// Gets or sets log buffer name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets duration to suspend buffering after the flush operation occurred.
    /// </summary>
    public TimeSpan? SuspendAfterFlushDuration { get; set; }

    /// <summary>
    /// Gets or sets a circular buffer duration.
    /// </summary>
    public TimeSpan? BufferingDuration { get; set; }

    /// <summary>
    /// Gets or sets buffer size.
    /// </summary>
    public long? BufferSize { get; set; }
}
