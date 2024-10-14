// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;
internal class GlobalFuncBasedSampler : LoggerSampler
{
    private readonly Func<SamplingParameters, bool>[] _samplingDecisions;

    public GlobalFuncBasedSampler(IEnumerable<Func<SamplingParameters, bool>> samplingDecisions)
    {
        _samplingDecisions = samplingDecisions.ToArray();
    }

    public override bool ShouldSample(in SamplingParameters parameters)
    {
        for (int i = 0; i < _samplingDecisions.Length; i++)
        {
            if (_samplingDecisions[i](parameters))
            {
                return true;
            }
        }

        return false;
    }
}
