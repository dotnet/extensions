// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// Represents a circular log buffer configuration.
/// </summary>
public class LogBufferConfig
{
    /// <summary>
    /// Gets or sets log buffer name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets duration to suspend buffering after the flush operation occurred.
    /// </summary>
    public TimeSpan? SuspendAfterFlushDuration { get; set; }

    /// <summary>
    /// Gets or sets a circular buffer duration.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Gets or sets buffer size.
    /// </summary>
    public long? Size { get; set; }
}
