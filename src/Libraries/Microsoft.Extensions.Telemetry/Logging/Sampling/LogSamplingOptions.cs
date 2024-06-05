// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Options to configure log sampling.
/// </summary>
public class LogSamplingOptions
{
    /// <summary>
    /// Gets or sets a list of log pattern matchers.
    /// </summary>
    public List<Matcher> Matchers { get; set; } = new List<Matcher>
    {
        new Matcher(
            new LogRecordPattern
            {
                LogLevel = LogLevel.Information,
                Category = "Microsoft.Extensions.Hosting",
            },
            // drop 99% of records:
            (pattern) => Random.Shared.NextDouble() < 0.01),
        new Matcher(
            new LogRecordPattern
            {
                LogLevel = LogLevel.Error,
            },
            // buffer records:
            (tool, pattern) => tool.Buffer("bufferName")),
    };

    /// <summary>
    /// Gets or sets a list of log buffers.
    /// </summary>
    public ISet<LogBuffer> Buffers { get; set; } = new HashSet<LogBuffer>
    {
        new LogBuffer
        {
            Name = "bufferName",
            SuspendAfterFlushDuration = TimeSpan.FromSeconds(10),
            BufferingDuration = TimeSpan.FromSeconds(10),
            BufferSize = 1_000_000,
        },
    };
}
