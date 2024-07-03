// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Logging.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Options to configure log sampling.
/// </summary>
public class BufferingFilterOptions
{
    /// <summary>
    /// Gets or sets a list of log pattern matchers.
    /// </summary>
    public IList<BufferingMatcher> Matchers { get; set; } = new List<BufferingMatcher>
    {
        new BufferingMatcher(
            new LogRecordPattern
            {
                LogLevel = LogLevel.Error,
            },

            // buffer records:
            (tool, pattern) => tool.Buffer("bufferName", pattern)),
    };
}
