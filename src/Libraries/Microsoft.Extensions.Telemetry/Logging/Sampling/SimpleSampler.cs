// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

internal class SimpleSampler : ILoggingSampler
{
    private readonly List<Matcher> _matchers;
    private readonly BufferingTool _bufferingTool;

    public SimpleSampler(IOptions<LogSamplingOptions> options, BufferingTool bufferingTool)
    {
        _matchers = options.Value.Matchers;
        _bufferingTool = bufferingTool;
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
                    case ControlAction.GlobalBuffer:
                        matcher.Buffer(_bufferingTool, logRecordPattern);
                        return true;
                    case ControlAction.RequestFilter:
                        break;
                    case ControlAction.RequestBuffer:
                        break;
                }
            }
        }

        return false;
    }
}
