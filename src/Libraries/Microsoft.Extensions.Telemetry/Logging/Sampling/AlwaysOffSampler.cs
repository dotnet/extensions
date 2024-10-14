// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// The Always On sampler, which does not sample at all, just drops all logs.
/// </summary>
public class AlwaysOffSampler : LoggerSampler
{
    /// <inheritdoc/>
    public override bool ShouldSample(in SamplingParameters parameters)
    {
        return false;
    }
}
