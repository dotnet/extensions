// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.Logging.Buffering;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

internal class SimpleBufferingFilter : ILogSampler
{
    private readonly IList<BufferingMatcher> _matchers;
    private readonly LogBuffer _bufferingTool;

    public SimpleBufferingFilter(IOptions<BufferingFilterOptions> options, LogBuffer bufferingTool)
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
                    case ControlAction.GlobalBuffer:
                        matcher!.Buffer(_bufferingTool, logRecordPattern);
                        return true;
                    case ControlAction.RequestBuffer:
                        break;
                }
            }
        }

        return false;
    }
}
