// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;
internal class TraceBasedSampler : LoggerSampler
{
    public override bool ShouldSample(SamplingParameters parameters) =>
        Activity.Current?.Recorded ?? false;
}
