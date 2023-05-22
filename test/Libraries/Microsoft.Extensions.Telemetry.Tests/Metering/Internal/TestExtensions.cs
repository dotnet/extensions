// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering.Test.Internal;

internal static class TestExtensions
{
    public static MeterProviderBuilder AddTestExporter(this MeterProviderBuilder builder, MetricReader reader)
    {
        return builder.AddReader(reader);
    }

#pragma warning disable S1751 // Loops with at most one iteration should be refactored
    public static MetricPoint First(this Metric metric)
    {
        foreach (var metricPoint in metric.GetMetricPoints())
        {
            return metricPoint;
        }

        return default;
    }

    public static Metric First(this Batch<Metric> metrics)
    {
        foreach (var metric in metrics)
        {
            return metric;
        }

        return null!;
    }

    public static Metric Get(this Batch<Metric> metrics, int index)
    {
        foreach (var metric in metrics)
        {
            if (--index == 0)
            {
                return metric;
            }
        }

        return null!;
    }

    public static Metric FirstMetric(this TestExporter testExporter)
    {
        return testExporter.Metrics.First();
    }

    public static MetricPoint FirstMetricPoint(this TestExporter testExporter)
    {
        return testExporter.FirstMetric().First();
    }

    public static MetricPoint FirstMetricPoint(this Batch<Metric> metrics)
    {
        return metrics.First().First();
    }
#pragma warning restore S1751 // Loops with at most one iteration should be refactored
}
