// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Sampling;

internal sealed class FuncBasedSampler : LoggerSampler
{
    private readonly Func<SamplingParameters, bool> _samplingDecisionFunc;

    public FuncBasedSampler(Func<SamplingParameters, bool> samplingDecisionFunc)
    {
        _samplingDecisionFunc = Throw.IfNull(samplingDecisionFunc);
    }

    public override bool ShouldSample(SamplingParameters parameters) => _samplingDecisionFunc(parameters);
}
