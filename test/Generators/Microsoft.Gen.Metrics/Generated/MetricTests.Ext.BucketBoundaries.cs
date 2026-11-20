// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET10_0_OR_GREATER
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

public partial class MetricTests
{
    [Fact]
    public void DoubleHistogramWithBuckets_AdvisesItsBoundaries()
    {
        using var collector = new MetricCollector<double>(_meter, nameof(DoubleHistogramWithBuckets));

        DoubleHistogramWithBuckets histogram = MetricsWithBucketBoundaries.CreateDoubleHistogramWithBuckets(_meter);
        histogram.Record(0.5, "v1");

        var measurement = Assert.Single(collector.GetMeasurementSnapshot());
        Assert.Equal(0.5, measurement.Value);

        var instrument = Assert.IsType<Histogram<double>>(collector.Instrument);
        Assert.Equal("s", instrument.Unit);
        Assert.NotNull(instrument.Advice);
        Assert.Equal([double.NegativeInfinity, 1E-05, 0.005, 0.1, 1, 10, double.PositiveInfinity], instrument.Advice.HistogramBucketBoundaries);
    }

    [Fact]
    public void LongHistogramWithBuckets_AdvisesItsBoundaries()
    {
        using var collector = new MetricCollector<long>(_meter, nameof(LongHistogramWithBuckets));

        LongHistogramWithBuckets histogram = MetricsWithBucketBoundaries.CreateLongHistogramWithBuckets(_meter);
        histogram.Record(42L);

        Assert.Equal(42L, Assert.Single(collector.GetMeasurementSnapshot()).Value);

        var instrument = Assert.IsType<Histogram<long>>(collector.Instrument);
        Assert.Equal("Long histogram with buckets.", instrument.Description);
        Assert.NotNull(instrument.Advice);
        Assert.Equal([long.MinValue, 0, 9_000_000_000], instrument.Advice.HistogramBucketBoundaries);
    }

    [Fact]
    public void FloatHistogramStrongTypeWithBuckets_AdvisesItsBoundaries()
    {
        using var collector = new MetricCollector<float>(_meter, nameof(FloatHistogramStrongTypeWithBuckets));

        FloatHistogramStrongTypeWithBuckets histogram = MetricsWithBucketBoundaries.CreateFloatHistogramStrongTypeWithBuckets(_meter);
        histogram.Record(1.5f, new Dimensions { Dim1 = "a", Dim2 = "b" });

        Assert.Equal(1.5f, Assert.Single(collector.GetMeasurementSnapshot()).Value);

        var instrument = Assert.IsType<Histogram<float>>(collector.Instrument);
        Assert.NotNull(instrument.Advice);
        Assert.Equal([0.1f, 0.25f, 2.5f], instrument.Advice.HistogramBucketBoundaries);
    }

    [Fact]
    public void IntHistogramWithoutBuckets_AdvisesNoBuckets()
    {
        using var collector = new MetricCollector<int>(_meter, nameof(IntHistogramWithoutBuckets));

        MetricsWithBucketBoundaries.CreateIntHistogramWithoutBuckets(_meter).Record(7);

        var instrument = Assert.IsType<Histogram<int>>(collector.Instrument);
        Assert.NotNull(instrument.Advice);
        Assert.NotNull(instrument.Advice.HistogramBucketBoundaries);
        Assert.Empty(instrument.Advice.HistogramBucketBoundaries);
    }
}
#endif
