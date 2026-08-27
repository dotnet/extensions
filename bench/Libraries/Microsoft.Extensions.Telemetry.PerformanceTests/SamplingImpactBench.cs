// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

[MemoryDiagnoser]
public class SamplingImpactBench
{
    private const int LogsPerMinute = 10_000;

    private static readonly Action<ILogger, int, Exception?> _logMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(1, "SamplingBenchmark"),
            "Sampling benchmark message {Value}");

    private ServiceProvider _baselineServices = null!;
    private ServiceProvider _sampledServices = null!;
    private ILogger _baselineLogger = null!;
    private ILogger _sampledLogger = null!;
    private Activity? _activity;

    public enum SamplingScenario
    {
        RandomSampleAll,
        RandomSampleOnePercent,
        RandomDropAll,
        TraceSample,
        TraceDrop
    }

    [Params(
        SamplingScenario.RandomSampleAll,
        SamplingScenario.RandomSampleOnePercent,
        SamplingScenario.RandomDropAll,
        SamplingScenario.TraceSample,
        SamplingScenario.TraceDrop)]
    public SamplingScenario Scenario { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _baselineServices = CreateServices();
        _sampledServices = CreateServices(Scenario);
        _baselineLogger = _baselineServices.GetRequiredService<ILoggerFactory>().CreateLogger("SamplingBenchmark");
        _sampledLogger = _sampledServices.GetRequiredService<ILoggerFactory>().CreateLogger("SamplingBenchmark");

        if (Scenario is SamplingScenario.TraceSample or SamplingScenario.TraceDrop)
        {
            _activity = new Activity("SamplingBenchmark")
            {
                ActivityTraceFlags = Scenario == SamplingScenario.TraceSample
                    ? ActivityTraceFlags.Recorded
                    : ActivityTraceFlags.None
            };
            _activity.Start();
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _activity?.Stop();
        _sampledServices.Dispose();
        _baselineServices.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = LogsPerMinute)]
    public void NoSampling()
    {
        for (int i = 0; i < LogsPerMinute; i++)
        {
            _logMessage(_baselineLogger, i, null);
        }
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void WithSampling()
    {
        for (int i = 0; i < LogsPerMinute; i++)
        {
            _logMessage(_sampledLogger, i, null);
        }
    }

    private static ServiceProvider CreateServices(SamplingScenario? scenario = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddProvider(new BenchLoggerProvider());

            switch (scenario)
            {
                case SamplingScenario.RandomSampleAll:
                    builder.AddRandomProbabilisticSampler(1.0);
                    break;
                case SamplingScenario.RandomSampleOnePercent:
                    builder.AddRandomProbabilisticSampler(0.01);
                    break;
                case SamplingScenario.RandomDropAll:
                    builder.AddRandomProbabilisticSampler(0.0);
                    break;
                case SamplingScenario.TraceSample:
                case SamplingScenario.TraceDrop:
                    builder.AddTraceBasedSampler();
                    break;
            }
        });

        return services.BuildServiceProvider();
    }
}
