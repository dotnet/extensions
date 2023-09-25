// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

internal static class LatencyContextControlExtensions
{
    public static bool TryGetCheckpoint(this ILatencyContext latencyContext, string checkpointName, out long elapsed, out long frequency)
    {
        var checkpoints = latencyContext.LatencyData.Checkpoints;
        foreach (var checkpoint in checkpoints)
        {
            if (string.Equals(checkpoint.Name, checkpointName, StringComparison.Ordinal))
            {
                elapsed = checkpoint.Elapsed;
                frequency = checkpoint.Frequency;
                return true;
            }
        }

        elapsed = 0;
        frequency = 0;
        return false;
    }
}
