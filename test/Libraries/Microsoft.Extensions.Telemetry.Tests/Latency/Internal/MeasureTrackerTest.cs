// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Diagnostics.Latency.Internal;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Latency.Test.Internal;

public class MeasureTrackerTest
{
    private static readonly Registry _measureNames = new(new[] { "a", "b", "c", "d" }, false);

    [Fact]
    public void MeasureTracker_AddUnregisteredName()
    {
        MeasureTracker mt = new MeasureTracker(_measureNames);
        mt.AddLong(mt.GetToken("e"), 10);
        Assert.True(mt.Measures.Count == 0);
        mt.SetLong(mt.GetToken("e"), 10);
        Assert.True(mt.Measures.Count == 0);
    }

    [Fact]
    public void MeasureTracker_AddRegisteredNames()
    {
        MeasureTracker mt = new MeasureTracker(_measureNames);
        string[] names = { "a", "b", "c" };
        int times = 3;

        for (int i = 0; i < names.Length; i++)
        {
            for (int j = 0; j < times; j++)
            {
                mt.AddLong(mt.GetToken(names[i]), i);
            }
        }

        var measures = mt.Measures.ToList();
        Assert.True(measures.Count == names.Length);

        // Verify measures have correct values.
        for (int i = 0; i < names.Length; i++)
        {
            var m = measures.Where(m => m.Name == names[i]).ToList();
            Assert.True(m.Count == 1);
            Assert.True(m[0].Name == names[i]);
            Assert.True(m[0].Value == i * times);
        }
    }

    [Fact]
    public void MeasureTracker_SetRegisteredNames()
    {
        MeasureTracker mt = new MeasureTracker(_measureNames);
        string[] names = { "a", "b", "c" };
        int times = 3;

        for (int i = 0; i < names.Length; i++)
        {
            for (int j = 0; j < times; j++)
            {
                mt.SetLong(mt.GetToken(names[i]), i);
            }
        }

        var measures = mt.Measures.ToArray();
        Assert.True(measures.Length == names.Length);

        for (int i = 0; i < names.Length; i++)
        {
            var m = measures.Where(m => m.Name == names[i]).ToList();
            Assert.True(m.Count == 1);
            Assert.True(m[0].Name == names[i]);
            Assert.True(m[0].Value == i);
        }
    }

    [Fact]
    public void MeasureTracker_Set_LasetSetWins()
    {
        MeasureTracker mt = new MeasureTracker(_measureNames);
        mt.AddLong(mt.GetToken("a"), 5);
        mt.SetLong(mt.GetToken("a"), 10);
        var measures = mt.Measures.ToList();
        Assert.NotNull(measures);
        Assert.True(measures.Count == 1);
        Assert.True(measures[0].Name == "a");
        Assert.True(measures[0].Value == 10);

        mt.SetLong(mt.GetToken("a"), 41);
        measures = mt.Measures.ToList();
        Assert.True(measures.Count == 1);
        Assert.True(measures[0].Name == "a");
        Assert.True(measures[0].Value == 41);
    }

    [Fact]
    public void MeasureTracker_CheckReset()
    {
        MeasureTracker mt = new MeasureTracker(_measureNames);
        mt.AddLong(mt.GetToken("a"), 5);
        mt.AddLong(mt.GetToken("b"), 3);
        mt.SetLong(mt.GetToken("c"), 10);

        Assert.True(mt.Measures.Count == 3);
        _ = mt.TryReset();
        Assert.True(mt.Measures.Count == 0);

        mt.AddLong(mt.GetToken("b"), 6);
        Assert.True(mt.Measures.Count == 1);
        var measures = mt.Measures.ToList();
        Assert.True(measures[0].Name == "b");
        Assert.True(measures[0].Value == 6);

        _ = mt.TryReset();
        Assert.True(mt.Measures.Count == 0);

        mt.AddLong(mt.GetToken("b"), 2);
        Assert.True(mt.Measures.Count == 1);
        measures = mt.Measures.ToList();
        Assert.True(measures[0].Name == "b");
        Assert.True(measures[0].Value == 2);
    }
}
