// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Metering.Test.Internal;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Metering.Test;

public class OTelMeteringExtensionsTests
{
    [Fact(Skip = "Flaky")]
    public void AddMetering_NullThrows()
    {
        MeterProviderBuilder nullBuilder = null!;
        IConfigurationRoot jsonConfigRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configurationSection = jsonConfigRoot.GetSection("Metering");

        var meteringOptions = new MeteringOptions();
        jsonConfigRoot.Bind("Metering", meteringOptions);
        configurationSection.Value = JsonConvert.SerializeObject(meteringOptions);

        Assert.Throws<ArgumentNullException>(() => nullBuilder.AddMetering());
        Assert.Throws<ArgumentNullException>(() => nullBuilder.AddMetering(options => { }));
        Assert.Throws<ArgumentNullException>(() => nullBuilder.AddMetering(configurationSection));
    }

    [Fact(Skip = "Flaky")]
    public async Task AddMetering_MetricPointsPerMetricStream_MoreThanConfigured_GetsDropped()
    {
        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder
                    .AddMetering(options =>
                    {
                        options.MaxMetricPointsPerStream = 3;
                    })
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName = $"testMeter{DateTime.Now.Ticks}";
        using var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>($"counter");

        for (int i = 0; i < 5; i++)
        {
            var tagList = new TagList
            {
                new KeyValuePair<string, object?>($"dim", $"value{i}")
            };

            counter.Add(1, tagList);
        }

        reader.Collect();

        Assert.Equal(1, exporter.Metrics.Count);
        Assert.Equal(meterName, exporter.Metrics.Get(1).MeterName);
        Assert.Equal("counter", exporter.Metrics.Get(1).Name);
        var metric = exporter.Metrics.Get(1);

        int metricPointsCount = 0;
        foreach (var metricPoint in metric.GetMetricPoints())
        {
            metricPointsCount++;
            Assert.Equal(1, metricPoint.Tags.Count);
        }

        Assert.Equal(2, metricPointsCount);
        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task AddMetering_MeterStateOverrides_GetsAppliedCorrectly()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();
        IConfigurationRoot jsonConfigRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configurationSection = jsonConfigRoot.GetSection("MeteringWithOverrides");

        var meteringOptions = new MeteringOptions();
        jsonConfigRoot.Bind("MeteringWithOverrides", meteringOptions);
        configurationSection.Value = JsonConvert.SerializeObject(meteringOptions);

        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder.AddMetering(configurationSection)
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName2 = $"testMeter2{DateTime.Now.Ticks}";
        using var meter2 = new Meter(meterName2);
        var counter2 = meter2.CreateCounter<long>("meter2_counter");

        var meterName3 = $"testMeter3{DateTime.Now.Ticks}";
        using var meter3 = new Meter(meterName3);
        var counter3 = meter3.CreateCounter<long>("meter3_counter");

        var tagList = new TagList
        {
            new KeyValuePair<string, object?>("dim1", "value1")
        };

        counter2.Add(7, tagList);
        counter3.Add(15, tagList);

        reader.Collect();
        Assert.Equal(1, exporter.Metrics.Count);
        Assert.Equal(meterName3, exporter.FirstMetric().MeterName);
        Assert.Equal("meter3_counter", exporter.FirstMetric().Name);
        Assert.Equal(MetricType.LongSum, exporter.FirstMetric().MetricType);
        Assert.Equal(15, exporter.FirstMetricPoint().GetSumLong());
        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task AddMetering_MeterStateOverrides_EmptyEntry_ShouldNotMatchAnyCategory()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();
        IConfigurationRoot jsonConfigRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configurationSection = jsonConfigRoot.GetSection("MeteringWithOverridesWithEmptyOverride");

        var meteringOptions = new MeteringOptions();
        jsonConfigRoot.Bind("MeteringWithOverridesWithEmptyOverride", meteringOptions);
        configurationSection.Value = JsonConvert.SerializeObject(meteringOptions);

        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder.AddMetering(configurationSection)
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName = $"testMeter{DateTime.Now.Ticks}";
        using var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("meter_counter");

        var tagList = new TagList
        {
            new KeyValuePair<string, object?>("dim1", "value1")
        };

        counter.Add(7, tagList);

        reader.Collect();
        Assert.Equal(1, exporter.Metrics.Count);
        Assert.Equal(meterName, exporter.FirstMetric().MeterName);
        Assert.Equal("meter_counter", exporter.FirstMetric().Name);
        Assert.Equal(MetricType.LongSum, exporter.FirstMetric().MetricType);
        Assert.Equal(7, exporter.FirstMetricPoint().GetSumLong());
        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task AddMetering_MeterStateOverrides_WithOverlappingMatches_AppliesBestMatch()
    {
        MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder();
        IConfigurationRoot jsonConfigRoot = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configurationSection = jsonConfigRoot.GetSection("MeteringWithOverrides");

        var meteringOptions = new MeteringOptions();
        jsonConfigRoot.Bind("MeteringWithOverrides", meteringOptions);
        configurationSection.Value = JsonConvert.SerializeObject(meteringOptions);

        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder.AddMetering(configurationSection)
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterNameR9Test = $"R9.Test{DateTime.Now.Ticks}";
        using var meter1 = new Meter(meterNameR9Test);
        var counter1 = meter1.CreateCounter<long>("counter1");
        counter1.Add(5);
        reader.Collect();
        Assert.Equal(0, exporter.Metrics.Count);
        Assert.False(IsMetricAvailable(exporter.Metrics, meterNameR9Test, "counter1"));

        var meterNameR9TestInternal = $"R9.Test.Internal{DateTime.Now.Ticks}";
        using var meter2 = new Meter(meterNameR9TestInternal);
        var counter2 = meter2.CreateCounter<long>("counter2");
        counter2.Add(16);
        reader.Collect();
        Assert.True(IsMetricAvailable(exporter.Metrics, meterNameR9TestInternal, "counter2"));
        Assert.Equal(16, exporter.FirstMetricPoint().GetSumLong());

        var meterNameR9TestExternal = $"R9.Test.External{DateTime.Now.Ticks}";
        using var meter3 = new Meter(meterNameR9TestExternal);
        var counter3 = meter3.CreateCounter<long>("counter3");
        counter2.Add(8);
        reader.Collect();
        Assert.False(IsMetricAvailable(exporter.Metrics, meterNameR9TestExternal, "counter3"));

        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task MeterState_Disabled_NoMetricsDisabled()
    {
        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder.AddMetering(options =>
                    {
                        options.MeterState = MeteringState.Disabled;
                    })
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName = $"testMeter{DateTime.Now.Ticks}";
        using var meter1 = new Meter(meterName);
        var counter1 = meter1.CreateCounter<long>("meter1_counter1");

        var tagList = new TagList
        {
            new KeyValuePair<string, object?>("dim1", "value1")
        };

        counter1.Add(10, tagList);
        reader.Collect();
        Assert.Null(exporter.FirstMetric());
        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task MeterState_Mixed_MetricsForOnlyEnabledMetersAreEmitted()
    {
        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder.AddMetering(options =>
                    {
                        options.MeterState = MeteringState.Disabled;
                        options.MeterStateOverrides.Add(new KeyValuePair<string, MeteringState>("testMeter2", MeteringState.Enabled));
                    })
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName = $"testMeter{DateTime.Now.Ticks}";
        using var meter1 = new Meter(meterName);
        var counter1 = meter1.CreateCounter<long>("meter1_counter1");

        var meterName2 = $"testMeter2{DateTime.Now.Ticks}";
        using var meter2 = new Meter(meterName2);
        var counter2 = meter2.CreateCounter<long>("meter2_counter");

        var tagList = new TagList
        {
            new KeyValuePair<string, object?>("dim1", "value1")
        };

        counter1.Add(10, tagList);

        counter2.Add(7, tagList);
        reader.Collect();
        Assert.Equal(1, exporter.Metrics.Count);
        Assert.Equal(meterName2, exporter.FirstMetric().MeterName);
        Assert.Equal("meter2_counter", exporter.FirstMetric().Name);
        Assert.Equal(MetricType.LongSum, exporter.FirstMetric().MetricType);
        Assert.Equal(7, exporter.FirstMetricPoint().GetSumLong());
        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task LongCounter_SumAsExpected()
    {
        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services
                .AddMetricEnricher<TestEnricher>()
                .AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder
                    .AddMetering()
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName = $"testMeter{DateTime.Now.Ticks}";
        using var meter1 = new Meter(meterName);

        var counter1 = meter1.CreateCounter<long>("meter1_counter1");

        var tagList = new TagList
        {
            new KeyValuePair<string, object?>("dim1", "value1")
        };

        counter1.Add(10, tagList);
        reader.Collect();
        Assert.Equal(meterName, exporter.FirstMetric().MeterName);
        Assert.Equal("meter1_counter1", exporter.FirstMetric().Name);
        Assert.Equal(MetricType.LongSum, exporter.FirstMetric().MetricType);
        Assert.Equal(10, exporter.FirstMetricPoint().GetSumLong());

        counter1.Add(10, tagList);
        reader.Collect();
        Assert.Equal(MetricType.LongSum, exporter.FirstMetric().MetricType);
        Assert.Equal(20, exporter.FirstMetricPoint().GetSumLong());
        Assert.Equal(tagList.Count, exporter.FirstMetricPoint().Tags.Count);

        var tags = exporter.FirstMetricPoint().Tags;

        foreach (var tag in tags)
        {
            Assert.Equal(tagList.First().Key, tag.Key);
            Assert.Equal(tagList.First().Value, tag.Value);
        }

        await host.StopAsync();
    }

    [Fact(Skip = "Flaky")]
    public async Task EmitMetric_DoesNotThrowAsync()
    {
        using var exporter = new TestExporter();
        using var reader = new BaseExportingMetricReader(exporter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
                services.AddOpenTelemetry().WithMetrics(builder =>
                {
                    builder.AddMetering(option =>
                    {
                        option.MeterState = MeteringState.Disabled;
                        option.MeterStateOverrides.Add(new KeyValuePair<string, MeteringState>("Microsoft.Extensions", MeteringState.Enabled));
                    })
                    .AddTestExporter(reader);
                }))
            .StartAsync();

        var meterName1 = $"Microsoft.Extensions.Meter1{DateTime.Now.Ticks}";
        var meterName2 = $"Meter2{DateTime.Now.Ticks}";
        using var meter1 = new Meter(meterName1);
        using var meter2 = new Meter(meterName2);

        var counter1 = meter1.CreateCounter<long>("meter1_counter1");
        var counter2 = meter2.CreateCounter<long>("meter2_counter1");

        var tagList = new TagList
        {
            new KeyValuePair<string, object?>("dim1", "value1")
        };

        counter1.Add(10, tagList);
        counter2.Add(50, tagList);

        reader.Collect();

        Assert.Equal(MetricType.LongSum, exporter.Metrics.First().MetricType);
        await host.StopAsync();
    }

    private static bool IsMetricAvailable(Batch<Metric> metrics, string meterName, string instrumentName)
    {
        foreach (var metric in metrics)
        {
            if (metric.MeterName == meterName && metric.Name == instrumentName)
            {
                return true;
            }
        }

        return false;
    }
}
