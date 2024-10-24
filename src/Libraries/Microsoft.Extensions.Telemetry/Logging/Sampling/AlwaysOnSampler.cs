// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;
internal class AlwaysOnSampler : LoggerSampler
{
    public override bool ShouldSample(SamplingParameters _) => true;
}
