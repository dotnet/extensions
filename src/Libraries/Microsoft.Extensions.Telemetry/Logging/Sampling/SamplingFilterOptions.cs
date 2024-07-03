// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Options to configure log sampling.
/// </summary>
public class SamplingFilterOptions
{
    /// <summary>
    /// Gets or sets a list of log pattern matchers.
    /// </summary>
    public IList<SamplingMatcher> Matchers { get; set; } = new List<SamplingMatcher>
    {
        new SamplingMatcher(
            new LogRecordPattern
            {
                LogLevel = LogLevel.Information,
                Category = "Microsoft.Extensions.Hosting",
            },

            // drop 99% of records:
            (pattern) => Random.Shared.NextDouble() < 0.01),
    };
}
