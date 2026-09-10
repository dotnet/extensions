// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.Sampling;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;

public class CckrTests
{
    [Fact]
    public void Flush_UsesFirstExcludedRankForSamplingCount()
    {
        const int Seed = 42;
        var sampler = new Cckr<int, int>(1, 0, 0, UnseenWeightMode.RarestSeen, Seed);

        Add(sampler, 0, 0);
        Add(sampler, 0, 1);

        SampledRecord<int, int> record = Assert.Single(sampler.Flush());

        var random = new Random(Seed);
        double firstRank = -Math.Log(random.NextDouble());
        double secondRank = -Math.Log(random.NextDouble());
        double firstExcludedRank = Math.Max(firstRank, secondRank);
        double expectedWeight = 1.0 / (1.0 - Math.Exp(-firstExcludedRank));

        Assert.Equal(expectedWeight, record.SamplingCount, 12);
        Assert.Equal(firstRank < secondRank ? 0 : 1, record.Payload);
    }

    [Fact]
    public void Flush_WhenInputFitsCapacity_UsesUnitWeights()
    {
        var sampler = new Cckr<int, int>(4, 0, 0, UnseenWeightMode.RarestSeen, 42);

        Add(sampler, 0, 0);
        Add(sampler, 1, 1);
        Add(sampler, 2, 2);

        List<SampledRecord<int, int>> records = sampler.Flush();

        Assert.Equal(3, records.Count);
        Assert.All(records, record => Assert.Equal(1.0, record.SamplingCount));
    }

    [Theory]
    [InlineData(1, 0.08)]
    [InlineData(8, 0.03)]
    [InlineData(32, 0.02)]
    [InlineData(128, 0.01)]
    public void SamplingCount_MeanConvergesToUniformInput(int capacity, double tolerance)
    {
        const int Arrivals = 256;
        const int Trials = 4_000;
        double estimatedTotal = 0.0;

        for (int seed = 0; seed < Trials; seed++)
        {
            var sampler = new Cckr<int, int>(capacity, 0, 0, UnseenWeightMode.RarestSeen, seed);
            for (int i = 0; i < Arrivals; i++)
            {
                Add(sampler, 0, i);
            }

            List<SampledRecord<int, int>> records = sampler.Flush();
            Assert.All(records, record =>
            {
                Assert.True(double.IsFinite(record.SamplingCount));
                Assert.True(record.SamplingCount >= 1.0);
            });

            estimatedTotal += records.Sum(record => record.SamplingCount);
        }

        double mean = estimatedTotal / Trials;
        Assert.InRange(mean, Arrivals * (1.0 - tolerance), Arrivals * (1.0 + tolerance));
    }

    [Fact]
    public void SamplingCount_MeanConvergesForSkewedKnownCallsites()
    {
        const int Trials = 2_000;
        int[] arrivals = [900, 90, 10];
        double[] estimatedByCallsite = new double[arrivals.Length];

        for (int seed = 0; seed < Trials; seed++)
        {
            var sampler = new Cckr<int, int>(32, 0, 0, UnseenWeightMode.RarestSeen, seed);

            AddPeriod(sampler, arrivals);
            _ = sampler.Flush();

            AddPeriod(sampler, arrivals);
            foreach (SampledRecord<int, int> record in sampler.Flush())
            {
                estimatedByCallsite[record.Callsite] += record.SamplingCount;
            }
        }

        for (int callsite = 0; callsite < arrivals.Length; callsite++)
        {
            double mean = estimatedByCallsite[callsite] / Trials;
            Assert.InRange(mean, arrivals[callsite] * 0.9, arrivals[callsite] * 1.1);
        }
    }

    private static void AddPeriod(Cckr<int, int> sampler, int[] arrivals)
    {
        for (int callsite = 0; callsite < arrivals.Length; callsite++)
        {
            for (int i = 0; i < arrivals[callsite]; i++)
            {
                Add(sampler, callsite, i);
            }
        }
    }

    private static void Add(Cckr<int, int> sampler, int callsite, int payload)
    {
        Admission admission = sampler.Admit(callsite);
        if (admission.Kind != AdmissionKind.Skip)
        {
            sampler.Insert(callsite, admission, payload);
        }
    }
}
#endif
