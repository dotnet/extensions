// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class AlwaysOnSampler : LoggerSampler
{
    public override bool ShouldSample(SamplingParameters _) => true;
}
