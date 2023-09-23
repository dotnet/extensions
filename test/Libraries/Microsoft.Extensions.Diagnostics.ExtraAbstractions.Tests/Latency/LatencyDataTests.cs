// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test;

public class LatencyDataTests
{
    [Fact]
    public void LatencyData_DefaultTest()
    {
        var ld = default(LatencyData);
        Assert.True(ld.Checkpoints.Length == 0);
        Assert.True(ld.Measures.Length == 0);
        Assert.True(ld.Tags.Length == 0);
    }

    [Fact]
    public void LatencyData_BasicTest()
    {
        int num = 5;

        ArraySegment<Checkpoint> checkpoints = new ArraySegment<Checkpoint>(LatencyDataTests.GetCheckpoints(num));
        ArraySegment<Measure> measures = new ArraySegment<Measure>(GetMeasures(num));
        ArraySegment<Tag> tags = new ArraySegment<Tag>(GetTags(num));

        var ld = new LatencyData(tags, checkpoints, measures, default, default);

        Assert.True(ld.Checkpoints.Length == num);
        Assert.True(ld.Measures.Length == num);
        Assert.True(ld.Tags.Length == num);
    }

    [Fact]
    public void LatencyData_SegmentTest()
    {
        int num = 6;
        int numCheckpoints = 3;
        int numMeasures = 1;
        int numTags = 2;

        ArraySegment<Checkpoint> checkpoints = new ArraySegment<Checkpoint>(LatencyDataTests.GetCheckpoints(num), 1, numCheckpoints);
        ArraySegment<Measure> measures = new ArraySegment<Measure>(GetMeasures(num), 2, numMeasures);
        ArraySegment<Tag> tags = new ArraySegment<Tag>(GetTags(num), 3, numTags);

        var ld = new LatencyData(tags, checkpoints, measures, default, default);

        Assert.True(ld.Checkpoints.Length == numCheckpoints);
        Assert.True(ld.Measures.Length == numMeasures);
        Assert.True(ld.Tags.Length == numTags);
    }

    private static Checkpoint[] GetCheckpoints(int length)
    {
        Checkpoint[] checkpoints = new Checkpoint[length];
        for (int i = 0; i < checkpoints.Length; i++)
        {
            checkpoints[i] = new Checkpoint("c" + i, default, default);
        }

        return checkpoints;
    }

    private static Measure[] GetMeasures(int length)
    {
        Measure[] measures = new Measure[length];
        for (int i = 0; i < measures.Length; i++)
        {
            measures[i] = new Measure("m" + i, i);
        }

        return measures;
    }

    private static Tag[] GetTags(int length)
    {
        Tag[] tags = new Tag[length];
        for (int i = 0; i < tags.Length; i++)
        {
            tags[i] = new Tag("tk" + i, "tv" + i);
        }

        return tags;
    }
}
