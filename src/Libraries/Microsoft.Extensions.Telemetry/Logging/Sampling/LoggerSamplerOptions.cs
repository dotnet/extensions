// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Logging.Buffering;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// The options for LoggerSampler.
/// </summary>
public class LoggerSamplerOptions
{
    /// <summary>
    /// Gets the collection of <see cref="LoggerFilterRule"/> used for filtering log messages.
    /// </summary>
    public IList<LoggerFilterRule> Rules => RulesInternal;

    // Concrete representation of the rule list
    internal List<LoggerFilterRule> RulesInternal { get; } = new List<LoggerFilterRule>();
}
