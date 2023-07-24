// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Gen.Metering.Model;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.Metering.Test;

public class EmitterTests
{
    [Fact]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
        foreach (var file in Directory.GetFiles("TestClasses"))
        {
            sources.Add(File.ReadAllText(file));
        }

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new MeteringGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(Meter))!,
                Assembly.GetAssembly(typeof(CounterAttribute))!,
                Assembly.GetAssembly(typeof(HistogramAttribute))!,
                Assembly.GetAssembly(typeof(CounterAttribute<>))!,
                Assembly.GetAssembly(typeof(HistogramAttribute<>))!,
            },
            sources)
            .ConfigureAwait(false);

        Assert.Empty(d);
        Assert.Equal(2, r.Length);

        string generatedContentPath = "GoldenFiles/Microsoft.Gen.Metering/Microsoft.Gen.Metering.MeteringGenerator";
        var goldenCache = File.ReadAllText($"{generatedContentPath}/Factory.g.cs");
        var goldenMetrics = File.ReadAllText($"{generatedContentPath}/Metering.g.cs");

        var result = r.First(x => x.HintName == "Factory.g.cs").SourceText.ToString();
        Assert.Equal(goldenCache, result);

        result = r.First(x => x.HintName == "Metering.g.cs").SourceText.ToString();
        Assert.Equal(goldenMetrics, result);
    }

    [Theory]
    [InlineData(10)]
    [InlineData((int)InstrumentKind.None)]
    [InlineData((int)InstrumentKind.Gauge)]
    public void EmitMeter_GivenMetricTypeIsUnknown_ThrowsNotSupportedException(int instrumentKind)
    {
        var metricClass = new MetricType
        {
            Name = "Logger",
            Namespace = "Samples",
            Methods =
            {
                new MetricMethod
                {
                    Name = "CreateUnknownMetric",
                    MetricName = "UnknownMetric",
                    MetricTypeName = "UnknownMetric",
                    InstrumentKind = (InstrumentKind)instrumentKind,
                    DimensionsKeys = { "Dim1" },
                    IsExtensionMethod = false,
                    Modifiers = "static partial",
                    AllParameters =
                    {
                        new MetricParameter
                        {
                            Name = "meter",
                            Type = "global::Microsoft.Extensions.Telemetry.Metering.IMeter",
                            IsMeter = true
                        }
                    }
                }
            }
        };

        var emitter = new Emitter();

        Assert.Throws<NotSupportedException>(() =>
            emitter.EmitMetrics(new List<MetricType> { metricClass }, cancellationToken: default));
    }
}
