// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;
internal class TraceBasedSampler : LoggerSampler
{
    public override bool ShouldSample(SamplingParameters _) =>
        Activity.Current?.Recorded ?? false;
}
