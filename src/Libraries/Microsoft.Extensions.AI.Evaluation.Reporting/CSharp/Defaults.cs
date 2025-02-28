// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// A static class that contains default values for various reporting artifacts.
/// </summary>
public static class Defaults
{
    /// <summary>
    /// The default execution name that should be used if one was not specified as part of the
    /// <see cref="ReportingConfiguration"/>.
    /// </summary>
    public const string DefaultExecutionName = "Default";

    /// <summary>
    /// The default iteration name that should be used if one was not specified when creating a
    /// <see cref="ScenarioRun"/> via
    /// <see cref="ReportingConfiguration.CreateScenarioRunAsync(string, string, IEnumerable{string}?, CancellationToken)"/>.
    /// </summary>
    public const string DefaultIterationName = "1";

    /// <summary>
    /// Gets a <see cref="TimeSpan"/> that specifies the default amount of time that cached AI responses should survive
    /// in the <see cref="IResponseCacheProvider"/>'s cache before they are considered expired and evicted.
    /// </summary>
    public static TimeSpan DefaultTimeToLiveForCacheEntries { get; } = TimeSpan.FromDays(14);
}
