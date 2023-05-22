// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Gen.Metering.Model;
using Xunit;

namespace Microsoft.Gen.MeteringReports.Test;

public class EmitterTests
{
    private static readonly ReportedMetricClass[] _metricClasses = new[]
        {
            new ReportedMetricClass
            {
                Name = "MetricClass1",
                RootNamespace = "MetricContainingAssembly1",
                Methods = new []
                {
                    new ReportedMetricMethod
                    {
                        MetricName = "Requests",
                        Summary = "Requests summary.",
                        Kind = InstrumentKind.Counter,
                        Dimensions = new() { "StatusCode", "ErrorCode"},
                        DimensionsDescriptions = new Dictionary<string, string>
                        {
                            { "StatusCode", "Status code for request." },
                            { "ErrorCode", "Error code for request." }
                        }
                    },
                    new ReportedMetricMethod
                    {
                        MetricName = "Latency",
                        Summary = "Latency summary.",
                        Kind = InstrumentKind.Histogram,
                        Dimensions = new() { "Dim1" },
                        DimensionsDescriptions = new()
                    },
                    new ReportedMetricMethod
                    {
                        MetricName = "MemoryUsage",
                        Kind = InstrumentKind.Gauge,
                        Dimensions = new(),
                        DimensionsDescriptions = new()
                    }
                }
            },
            new ReportedMetricClass
            {
                Name = "MetricClass2",
                RootNamespace = "MetricContainingAssembly2",
                Methods = new[]
                {
                    new ReportedMetricMethod
                    {
                        MetricName = "Counter",
                        Summary = "Counter summary.",
                        Kind = InstrumentKind.Counter,
                        Dimensions = new(),
                        DimensionsDescriptions = new()
                    },
                    new ReportedMetricMethod
                    {
                        MetricName = "R9\\Test\\MemoryUsage",
                        Summary = "MemoryUsage summary.",
                        Kind = InstrumentKind.Gauge,
                        Dimensions = new() { "Path"},
                        DimensionsDescriptions = new Dictionary<string, string>
                        {
                            { "Path", "R9\\Test\\Description\\Path" }
                        },
                    }
                }
            }
    };

    [Fact]
    public void EmitterShouldThrowExceptionUponCancellation()
    {
        Assert.Throws<OperationCanceledException>(() => MetricDefinitionEmitter.GenerateReport(_metricClasses, new CancellationToken(true)));
    }

    [Fact]
    public void EmitterShouldOutputEmptyForNullInput()
    {
        Assert.Equal(string.Empty, MetricDefinitionEmitter.GenerateReport(null!, CancellationToken.None));
    }

    [Fact]
    public void EmitterShouldOutputEmptyForEmptyInputForMetricClass()
    {
        Assert.Equal(string.Empty, MetricDefinitionEmitter.GenerateReport(Array.Empty<ReportedMetricClass>(), CancellationToken.None));
    }

    [Fact]
    public void GetMetricClassDefinition_GivenMetricTypeIsUnknown_ThrowsNotSupportedException()
    {
        const int UnknownMetricType = 10;

        var metricClass = new ReportedMetricClass
        {
            Name = "Test",
            RootNamespace = "MetricContainingAssembly3",
            Methods = new[]
            {
                new ReportedMetricMethod
                {
                    MetricName = "UnknownMetric",
                    Kind = (InstrumentKind)UnknownMetricType,
                    Dimensions = new() { "Dim1" }
                }
            }
        };

        Assert.Throws<NotSupportedException>(() => MetricDefinitionEmitter.GenerateReport(new[] { metricClass }, CancellationToken.None));
    }

    [Fact]
    public void EmitterShouldOutputInJSONFormat()
    {
        const string Expected =
            "[" +
            "\n {" +
            "\n  \"MetricContainingAssembly1\":" +
            "\n  [" +
            "\n    {" +
            "\n     \"MetricName\": \"Requests\"," +
            "\n     \"MetricDescription\": \"Requests summary.\"," +
            "\n     \"InstrumentName\": \"Counter\"," +
            "\n     \"Dimensions\": {" +
            "\n      \"StatusCode\": \"Status code for request.\"," +
            "\n      \"ErrorCode\": \"Error code for request.\"" +
            "\n      }" +
            "\n    }," +
            "\n    {" +
            "\n     \"MetricName\": \"Latency\"," +
            "\n     \"MetricDescription\": \"Latency summary.\"," +
            "\n     \"InstrumentName\": \"Histogram\"," +
            "\n     \"Dimensions\": {" +
            "\n      \"Dim1\": \"\"" +
            "\n      }" +
            "\n    }," +
            "\n    {" +
            "\n     \"MetricName\": \"MemoryUsage\"," +
            "\n     \"InstrumentName\": \"Gauge\"" +
            "\n    }" +
            "\n  ]" +
            "\n }," +
            "\n {" +
            "\n  \"MetricContainingAssembly2\":" +
            "\n  [" +
            "\n    {" +
            "\n     \"MetricName\": \"Counter\"," +
            "\n     \"MetricDescription\": \"Counter summary.\"," +
            "\n     \"InstrumentName\": \"Counter\"" +
            "\n    }," +
            "\n    {" +
            "\n     \"MetricName\": \"R9\\\\Test\\\\MemoryUsage\"," +
            "\n     \"MetricDescription\": \"MemoryUsage summary.\"," +
            "\n     \"InstrumentName\": \"Gauge\"," +
            "\n     \"Dimensions\": {" +
            "\n      \"Path\": \"R9\\\\Test\\\\Description\\\\Path\"" +
            "\n      }" +
            "\n    }" +
            "\n  ]" +
            "\n }" +
            "\n]";

        string json = MetricDefinitionEmitter.GenerateReport(_metricClasses, CancellationToken.None);

        Assert.Equal(Expected, json);
    }
}
