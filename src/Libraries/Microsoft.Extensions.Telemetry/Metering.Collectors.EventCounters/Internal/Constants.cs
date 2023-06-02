// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Metering.Internal;

internal sealed class Constants
{
#pragma warning disable R9A044 // Assign array of literal values to a static field for improved performance
    public static readonly Dictionary<string, HashSet<string>> DefaultCounters = new()
    {
        ["System.Runtime"] = new HashSet<string>
        {
            "cpu-usage",
            "working-set",
            "time-in-gc",
            "alloc-rate",
            "exception-count",
            "gen-2-gc-count",
            "gen-2-size",
            "monitor-lock-contention-count",
            "active-timer-count",
            "threadpool-queue-length",
            "threadpool-thread-count"
        },
        ["Microsoft-AspNetCore-Server-Kestrel"] = new HashSet<string>
        {
            "connection-queue-length",
            "request-queue-length",
        },
    };
#pragma warning restore R9A044 // Assign array of literal values to a static field for improved performance
}
