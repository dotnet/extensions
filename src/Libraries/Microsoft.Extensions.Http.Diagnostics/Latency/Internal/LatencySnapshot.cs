// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.Http.Latency.Internal;

internal sealed class LatencySnapshot(in LatencyData data)
{
    public Tag[] Tags { get; } = CopyTags(data.Tags);
    public Checkpoint[] Checkpoints { get; } = CopyCheckpoints(data.Checkpoints);
    public Measure[] Measures { get; } = CopyMeasures(data.Measures);
    public long DurationTimestamp { get; } = data.DurationTimestamp;
    public long DurationTimestampFrequency { get; } = data.DurationTimestampFrequency;

    private static Tag[] CopyTags(ReadOnlySpan<Tag> src)
    {
        if (src.IsEmpty)
        {
            return [];
        }

        var arr = new Tag[src.Length];
        src.CopyTo(arr);
        return arr;
    }

    private static Checkpoint[] CopyCheckpoints(ReadOnlySpan<Checkpoint> src)
    {
        if (src.IsEmpty)
        {
            return [];
        }

        var arr = new Checkpoint[src.Length];
        src.CopyTo(arr);
        return arr;
    }

    private static Measure[] CopyMeasures(ReadOnlySpan<Measure> src)
    {
        if (src.IsEmpty)
        {
            return [];
        }

        var arr = new Measure[src.Length];
        src.CopyTo(arr);
        return arr;
    }
}