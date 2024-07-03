// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Logging.Buffering;

/// <summary>
/// Options to configure log buffering.
/// </summary>
public class LogBufferingOptions
{
    /// <summary>
    /// Gets or sets a list of log buffers.
    /// </summary>
    public ISet<LogBufferConfig> Configs { get; set; } = new HashSet<LogBufferConfig>();
}
