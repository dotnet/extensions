// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Represents a component that samples log records.
/// </summary>
public abstract class LoggerSampler
{
    /// <summary>
    /// Makes a sampling decision based on the provided parameters.
    /// </summary>
    /// <returns>True, if the log record should be sampled. False otherwise.</returns>
    public abstract bool ShouldSample(in SamplingParameters parameters);
}
