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

        // The golden files come from the Generated project, which compiles MetricsWithBucketBoundaries only on .NET 10 and later.
#if NET10_0_OR_GREATER
        string[] preprocessorSymbols = ["NET10_0_OR_GREATER"];
#else
        string[] preprocessorSymbols = [];
#endif

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
            sources,
            preprocessorSymbols)
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

    [Theory]
    [InlineData(null, null, "@\"TestMetric\"")]
    [InlineData("", "", "@\"TestMetric\"")]
    [InlineData("ms", null, "@\"TestMetric\", @\"ms\"")]
    [InlineData(null, "Some description.", "@\"TestMetric\", null, \"Some description.\"")]
    [InlineData("ms", "Some description.", "@\"TestMetric\", @\"ms\", \"Some description.\"")]
    [InlineData(null, "Has \"quotes\", \\ and\nnew line.", "@\"TestMetric\", null, \"Has \\\"quotes\\\", \\\\ and\\nnew line.\"")]
    public void EmitFactory_EmitsExpectedInstrumentArguments(string? unit, string? description, string expectedArguments)
    {
        var result = EmitFactory(InstrumentKind.Counter, unit, description);

        Assert.Contains($"var instrument = _meter.CreateCounter<long>({expectedArguments});", result);
    }

    [Theory]
    [InlineData((int)InstrumentKind.Counter, "CreateCounter")]
    [InlineData((int)InstrumentKind.CounterT, "CreateCounter")]
    [InlineData((int)InstrumentKind.Histogram, "CreateHistogram")]
    [InlineData((int)InstrumentKind.HistogramT, "CreateHistogram")]
    [InlineData((int)InstrumentKind.Gauge, "CreateGauge")]
    [InlineData((int)InstrumentKind.GaugeT, "CreateGauge")]
    public void EmitFactory_WithDescription_PassesDescriptionForAllInstrumentKinds(int instrumentKind, string createMethodName)
    {
        var result = EmitFactory((InstrumentKind)instrumentKind, unit: null, description: "Some description.");

        Assert.Contains($"var instrument = _meter.{createMethodName}<long>(@\"TestMetric\", null, \"Some description.\");", result);
    }

    [Fact]
    public async Task Generator_GaugeWithDescription_PassesDescriptionToInstrument()
    {
        var source = @"
            namespace Test
            {
                using System.Diagnostics.Metrics;
                using Microsoft.Extensions.Diagnostics.Metrics;

                public class GaugeTags
                {
                    public string? Region { get; set; }
                }

                public static partial class GaugeMetrics
                {
                    [Gauge(""region"", Description = ""Gauge description."")]
                    public static partial MyGauge CreateGauge(Meter meter);

                    [Gauge<double>(Unit = ""By"", Description = ""Generic gauge description."")]
                    public static partial MyGenericGauge CreateGenericGauge(Meter meter);

                    [Gauge(typeof(GaugeTags), Description = ""Strongly-typed gauge description."")]
                    public static partial MyStrongTypeGauge CreateStrongTypeGauge(Meter meter);
                }
            }";

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new MetricsGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(Meter))!,
                Assembly.GetAssembly(typeof(GaugeAttribute))!,
            },
            new[] { source });

        Assert.Empty(d);

        var factory = r.First(x => x.HintName == "Factory.g.cs").SourceText.ToString();
        Assert.Contains("_meter.CreateGauge<long>(@\"MyGauge\", null, \"Gauge description.\");", factory);
        Assert.Contains("_meter.CreateGauge<double>(@\"MyGenericGauge\", @\"By\", \"Generic gauge description.\");", factory);
        Assert.Contains("_meter.CreateGauge<long>(@\"MyStrongTypeGauge\", null, \"Strongly-typed gauge description.\");", factory);
    }

    [Fact]
    public void EmitFactory_WithBucketBoundaries_PassesThemAsAdvice()
    {
        var result = EmitFactory(InstrumentKind.HistogramT, unit: "s", description: "Some description.", "double", 0.005, 0.1, 1d, 10d);

        Assert.Contains("var instrument = _meter.CreateHistogram<double>(@\"TestMetric\", @\"s\", \"Some description.\", tags: null,", result);
        Assert.Contains(
            "advice: new global::System.Diagnostics.Metrics.InstrumentAdvice<double> { HistogramBucketBoundaries = new double[] { 0.005D, 0.1D, 1D, 10D } });",
            result);
    }

    [Theory]
    [InlineData("double", "new double[] { double.NegativeInfinity, -0.5D, 1E-05D, 1E+20D, double.PositiveInfinity }",
        double.NegativeInfinity, -0.5, 1E-05, 1E+20, double.PositiveInfinity)]
    [InlineData("float", "new float[] { float.NegativeInfinity, 0.1F, 2.5F, float.PositiveInfinity }",
        float.NegativeInfinity, 0.1f, 2.5f, float.PositiveInfinity)]
    [InlineData("long", "new long[] { -9223372036854775808L, 0L, 9000000000L }", long.MinValue, 0L, 9_000_000_000L)]
    [InlineData("int", "new int[] { -5, 100 }", -5, 100)]
    [InlineData("short", "new short[] { -5, 100 }", (short)-5, (short)100)]
    [InlineData("byte", "new byte[] { 1, 255 }", (byte)1, (byte)255)]
    [InlineData("double", "global::System.Array.Empty<double>()")]
    public void EmitFactory_WritesEachBucketBoundaryAsALiteralOfItsType(string genericType, string expected, params object[] bucketBoundaries)
    {
        var result = EmitFactory(InstrumentKind.HistogramT, unit: null, description: null, genericType, bucketBoundaries);

        Assert.Contains($"var instrument = _meter.CreateHistogram<{genericType}>(@\"TestMetric\", null, null, tags: null,", result);
        Assert.Contains($"{{ HistogramBucketBoundaries = {expected} }});", result);
    }

    [Fact]
    public void EmitFactory_GivenBucketBoundaryTypeIsUnknown_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => EmitFactory(InstrumentKind.HistogramT, unit: null, description: null, "decimal", 1m));
    }

    private static string EmitFactory(InstrumentKind instrumentKind, string? unit, string? description)
        => EmitFactory(instrumentKind, unit, description, "long", bucketBoundaries: null);

    private static string EmitFactory(InstrumentKind instrumentKind, string? unit, string? description, string genericType,
        params object[]? bucketBoundaries)
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
                    MetricTypeName = "TestMetric",
                    MetricUnit = unit,
                    MetricDescription = description,
                    HistogramBucketBoundaries = bucketBoundaries,
                    InstrumentKind = instrumentKind,
                    GenericType = genericType,
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

        return new MetricFactoryEmitter().Emit(new List<MetricType> { metricClass }, cancellationToken: default);
    }
}
