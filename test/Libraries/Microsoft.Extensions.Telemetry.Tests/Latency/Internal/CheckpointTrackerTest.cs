// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

public class CheckpointTrackerTest
{
    private static readonly Registry _checkpointsName = new(new[] { "a", "b", "c", "d" }, false);

    [Fact]
    public void CheckpointTracker_AddUnregisteredName()
    {
        CheckpointTracker ct = new CheckpointTracker(_checkpointsName);
        ct.Add(ct.GetToken("e"));
        Assert.True(ct.Checkpoints.Count == 0);
    }

    [Fact]
    public void CheckpointTracker_AddRegisteredNames()
    {
        CheckpointTracker ct = new CheckpointTracker(_checkpointsName);
        var t = ct.Elapsed;
        string[] names = { "a", "b", "c" };

        for (int i = 0; i < names.Length; i++)
        {
            ct.Add(ct.GetToken(names[i]));
        }

        var c = ct.Checkpoints.ToList();
        Assert.True(c.Count == names.Length);

        for (int i = 0; i < names.Length; i++)
        {
            var elapsed = c[i].Elapsed;

            // Verify names are in order and timestamp ascending
            Assert.True(c[i].Name == names[i]);
            Assert.True(elapsed >= t);
            t = elapsed;
        }
    }

    [Fact]
    public void CheckpointTracker_AddDuplicateNames_FirstWriteWins()
    {
        CheckpointTracker ct = new CheckpointTracker(_checkpointsName);
        ct.Add(ct.GetToken("a"));
        var first = ct.Checkpoints.First();
        ct.Add(ct.GetToken("a"));

        // Verify value unchanged
        var checkpoints = ct.Checkpoints.ToList();
        Assert.True(checkpoints.Count == 1);
        Assert.True(first.Name == checkpoints[0].Name);
        Assert.True(first.Elapsed == checkpoints[0].Elapsed);
        Assert.True(first.Frequency == checkpoints[0].Frequency);
    }

    [Fact]
    public void CheckpointTracker_CheckReset()
    {
        CheckpointTracker ct = new CheckpointTracker(_checkpointsName);
        string[] names = { "a", "b", "c" };

        for (int i = 0; i < names.Length; i++)
        {
            ct.Add(ct.GetToken(names[i]));
        }

        var c = ct.Checkpoints;
        Assert.True(c.Count == names.Length);

        _ = ct.TryReset();
        c = ct.Checkpoints;
        Assert.True(c.Count == 0);

        for (int i = 0; i < names.Length; i++)
        {
            ct.Add(ct.GetToken(names[i]));
        }

        c = ct.Checkpoints;
        Assert.True(c.Count == names.Length);
    }
}
