// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

using static Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Telemetry.Metering.Test.Auxiliary;

public static class TestUtils
{
    public const string SystemRuntime = "System.Runtime";

    public static IOptions<EventCountersCollectorOptions> CreateOptions(string eventSource, string counterName)
    {
        var options = Create(new EventCountersCollectorOptions());
        options.Value.Counters.Add(eventSource, new HashSet<string> { counterName });
        options.Value.SamplingInterval = TimeSpan.FromMilliseconds(1);

        return options;
    }
}
