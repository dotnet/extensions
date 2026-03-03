// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Gen.Metrics.Model;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.Metrics.Test;

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
            new MetricsGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(Meter))!,
                Assembly.GetAssembly(typeof(CounterAttribute))!,
                Assembly.GetAssembly(typeof(HistogramAttribute))!,
                Assembly.GetAssembly(typeof(CounterAttribute<>))!,
                Assembly.GetAssembly(typeof(HistogramAttribute<>))!,
                Assembly.GetAssembly(typeof(GaugeAttribute))!,
                Assembly.GetAssembly(typeof(GaugeAttribute<>))!,
            },
            sources)
;

        Assert.Empty(d);
        Assert.Equal(2, r.Length);

        string generatedContentPath = "GoldenFiles/Microsoft.Gen.Metrics/Microsoft.Gen.Metrics.MetricsGenerator";
        var goldenCache = File.ReadAllText($"{generatedContentPath}/Factory.g.cs");
        var goldenMetrics = File.ReadAllText($"{generatedContentPath}/Metrics.g.cs");

        var result = r.First(x => x.HintName == "Factory.g.cs").SourceText.ToString();
        Assert.Equal(goldenCache, result);

        result = r.First(x => x.HintName == "Metrics.g.cs").SourceText.ToString();
        Assert.Equal(goldenMetrics, result);
    }

    [Theory]
    [InlineData(10)]
    [InlineData((int)InstrumentKind.None)]
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
                    TagKeys = { "Dim1" },
                    IsExtensionMethod = false,
                    Modifiers = "static partial",
                    AllParameters =
                    {
                        new MetricParameter
                        {
                            Name = "meter",
                            Type = "global::Microsoft.Extensions.Diagnostics.Metrics.IMeter",
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

    [Theory]
    [InlineData(10)]
    [InlineData((int)InstrumentKind.None)]
    public void EmitFactory_GivenMetricTypeIsUnknown_ThrowsNotSupportedException(int instrumentKind)
    {
        var metricClass = new MetricType
        {
            Name = "MetricClass",
            Namespace = "Samples",
            Methods =
            {
                new MetricMethod
                {
                    Name = "CreateUnknownMetric",
                    MetricName = "UnknownMetric",
                    MetricTypeName = "UnknownMetric",
                    InstrumentKind = (InstrumentKind)instrumentKind,
                    GenericType = "long",
                    IsExtensionMethod = false,
                    Modifiers = "static partial",
                    MetricTypeModifiers = "public static partial",
                    AllParameters =
                    {
                        new MetricParameter
                        {
                            Name = "meter",
                            Type = "global::System.Diagnostics.Metrics.Meter",
                            IsMeter = true
                        }
                    }
                }
            }
        };

        var emitter = new MetricFactoryEmitter();

        Assert.Throws<NotSupportedException>(() =>
            emitter.Emit(new List<MetricType> { metricClass }, cancellationToken: default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public void EmitFactory_MetricTypeNameWithLowercaseOrEmptyFirstChar_DoesNotThrow(string metricTypeName)
    {
        var metricClass = new MetricType
        {
            Name = "MetricClass",
            Namespace = "Samples",
            Methods =
            {
                new MetricMethod
                {
                    Name = "CreateMetric",
                    MetricName = "TestMetric",
                    MetricTypeName = metricTypeName,
                    InstrumentKind = InstrumentKind.Counter,
                    GenericType = "long",
                    IsExtensionMethod = false,
                    Modifiers = "static partial",
                    MetricTypeModifiers = "public static partial",
                    AllParameters =
                    {
                        new MetricParameter
                        {
                            Name = "meter",
                            Type = "global::System.Diagnostics.Metrics.Meter",
                            IsMeter = true
                        }
                    }
                }
            }
        };

        var emitter = new MetricFactoryEmitter();
        var result = emitter.Emit(new List<MetricType> { metricClass }, cancellationToken: default);

        Assert.NotNull(result);
    }

    [Fact]
    public void EmitFactory_MetricTypeNameWithSingleUppercaseChar_DoesNotThrow()
    {
        var metricClass = new MetricType
        {
            Name = "MetricClass",
            Namespace = "Samples",
            Methods =
            {
                new MetricMethod
                {
                    Name = "CreateMetric",
                    MetricName = "TestMetric",
                    MetricTypeName = "M",
                    InstrumentKind = InstrumentKind.Counter,
                    GenericType = "long",
                    IsExtensionMethod = false,
                    Modifiers = "static partial",
                    MetricTypeModifiers = "public static partial",
                    AllParameters =
                    {
                        new MetricParameter
                        {
                            Name = "meter",
                            Type = "global::System.Diagnostics.Metrics.Meter",
                            IsMeter = true
                        }
                    }
                }
            }
        };

        var emitter = new MetricFactoryEmitter();
        var result = emitter.Emit(new List<MetricType> { metricClass }, cancellationToken: default);

        Assert.NotNull(result);
        Assert.Contains("_mInstruments", result);
    }
}
