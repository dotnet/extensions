// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

internal class SimpleSamplingFilter : ILogSampler
{
    private readonly IList<SamplingMatcher> _matchers;

    public SimpleSamplingFilter(IOptions<SamplingFilterOptions> options)
    {
        _matchers = options.Value.Matchers;
    }

    public bool Sample(LogRecordPattern logRecordPattern)
    {
        foreach (var matcher in _matchers)
        {
            if (matcher.Match(logRecordPattern))
            {
                switch (matcher.ControlAction)
                {
                    case ControlAction.GlobalFilter:
                        if (!matcher.Filter(logRecordPattern))
                        {
                            return true;
                        }

                        break;
                    case ControlAction.RequestFilter:
                        break;
                }
            }
        }

        return false;
    }
}
