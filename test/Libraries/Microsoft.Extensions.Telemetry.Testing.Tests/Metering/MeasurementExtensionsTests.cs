// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Testing.Metering.Test;

public static class MeasurementExtensionsTests
{
    [Fact]
    public static void ContainsTagKeys()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>("MyCounter");
        using var collector = new MetricCollector<long>(counter);

        counter.Add(1, new("A", "a"), new("B", "b"));
        counter.Add(2, new("A", "a"), new("B", "b"));
        counter.Add(3, new("X", "x"), new("Y", "y"));

        var fullSnap = collector.GetMeasurementSnapshot();

        var filtered = fullSnap.ContainsTags("A").ToList();
        Assert.Equal(2, filtered.Count);
        Assert.Equal(1, filtered[0].Value);
        Assert.Equal(2, filtered[1].Value);
        Assert.Equal(2, filtered[1].Tags.Count);
        Assert.True(filtered[1].Tags.ContainsKey("A"));
        Assert.True(filtered[1].Tags.ContainsKey("B"));

        filtered = fullSnap.ContainsTags("M").ToList();
        Assert.Empty(filtered);
    }

    [Fact]
    public static void ContainsTags()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>("MyCounter");
        using var collector = new MetricCollector<long>(counter);

        counter.Add(1, new("A", "a"), new("B", "b"));
        counter.Add(2, new("A", "a"), new("B", "b"));
        counter.Add(3, new("X", "x"), new("Y", "y"));

        var fullSnap = collector.GetMeasurementSnapshot();

        var filtered = fullSnap.ContainsTags(new KeyValuePair<string, object?>("A", "a")).ToList();
        Assert.Equal(2, filtered.Count);
        Assert.Equal(1, filtered[0].Value);
        Assert.Equal(2, filtered[1].Value);
        Assert.Equal(2, filtered[1].Tags.Count);
        Assert.True(filtered[1].Tags.ContainsKey("A"));
        Assert.True(filtered[1].Tags.ContainsKey("B"));

        filtered = fullSnap.ContainsTags(new KeyValuePair<string, object?>("A", "X")).ToList();
        Assert.Empty(filtered);
    }

    [Fact]
    public static void MatchesTagKeys()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>("MyCounter");
        using var collector = new MetricCollector<long>(counter);

        counter.Add(1, new("A", "a"), new("B", "b"));
        counter.Add(2, new("A", "a"), new("B", "b"));
        counter.Add(3, new("X", "x"), new("Y", "y"));

        var fullSnap = collector.GetMeasurementSnapshot();

        var filtered = fullSnap.MatchesTags("A", "B").ToList();
        Assert.Equal(2, filtered.Count);
        Assert.Equal(1, filtered[0].Value);
        Assert.Equal(2, filtered[1].Value);
        Assert.Equal(2, filtered[1].Tags.Count);
        Assert.True(filtered[1].Tags.ContainsKey("A"));
        Assert.True(filtered[1].Tags.ContainsKey("B"));

        filtered = fullSnap.MatchesTags("A").ToList();
        Assert.Empty(filtered);

        filtered = fullSnap.MatchesTags("A", "B", "C").ToList();
        Assert.Empty(filtered);
    }

    [Fact]
    public static void MatchesTags()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>("MyCounter");
        using var collector = new MetricCollector<long>(counter);

        counter.Add(1, new("A", "a"), new("B", "b"));
        counter.Add(2, new("A", "a"), new("B", "b"));
        counter.Add(3, new("X", "x"), new("Y", "y"));

        var fullSnap = collector.GetMeasurementSnapshot();

        var filtered = fullSnap.MatchesTags(new KeyValuePair<string, object?>("A", "a"), new("B", "b")).ToList();
        Assert.Equal(2, filtered.Count);
        Assert.Equal(1, filtered[0].Value);
        Assert.Equal(2, filtered[1].Value);
        Assert.Equal(2, filtered[1].Tags.Count);
        Assert.True(filtered[1].Tags.ContainsKey("A"));
        Assert.True(filtered[1].Tags.ContainsKey("B"));

        filtered = fullSnap.MatchesTags(new KeyValuePair<string, object?>("A", "a")).ToList();
        Assert.Empty(filtered);

        filtered = fullSnap.MatchesTags(new KeyValuePair<string, object?>("A", "a"), new("B", "x")).ToList();
        Assert.Empty(filtered);

        filtered = fullSnap.MatchesTags(new KeyValuePair<string, object?>("A", "a"), new("B", "b"), new("C", "c")).ToList();
        Assert.Empty(filtered);
    }

    [Fact]
    public static void SumBytes()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<byte>("MyCounter");
        using var collector = new MetricCollector<byte>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(200);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(147, total);
    }

    [Fact]
    public static void SumShorts()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<short>("MyCounter");
        using var collector = new MetricCollector<short>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(32760);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(-32573, total);
    }

    [Fact]
    public static void SumInts()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<int>("MyCounter");
        using var collector = new MetricCollector<int>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(32760);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(32963, total);
    }

    [Fact]
    public static void SumLongs()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<long>("MyCounter");
        using var collector = new MetricCollector<long>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(32760);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(32963, total);
    }

    [Fact]
    public static void SumFloats()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<float>("MyCounter");
        using var collector = new MetricCollector<float>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(32760);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(32963, total);
    }

    [Fact]
    public static void SumDouble()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<double>("MyCounter");
        using var collector = new MetricCollector<double>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(32760);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(32963, total);
    }

    [Fact]
    public static void SumDecimals()
    {
        using var meter = new Meter(Guid.NewGuid().ToString());
        var counter = meter.CreateCounter<decimal>("MyCounter");
        using var collector = new MetricCollector<decimal>(counter);

        counter.Add(1);
        counter.Add(2);
        counter.Add(200);
        counter.Add(32760);

        var fullSnap = collector.GetMeasurementSnapshot();
        var total = fullSnap.EvaluateAsCounter();
        Assert.Equal(32963, total);
    }
}
