// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

/// <summary>
/// Measures the incremental cost of evaluating CCKR retain-all log-level policies.
/// </summary>
[MemoryDiagnoser]
[InvocationCount(1)]
public class CckrLogLevelPolicyBench
{
    private const int AdaptiveCapacity = 128;

    private ServiceProvider _withoutLogLevelPolicyServices = null!;
    private ServiceProvider _withLogLevelPolicyServices = null!;
    private ILogger[] _withoutLogLevelPolicyLoggers = null!;
    private ILogger[] _withLogLevelPolicyLoggers = null!;
    private LogBuffer _withoutLogLevelPolicyBuffer = null!;
    private LogBuffer _withLogLevelPolicyBuffer = null!;

    [Params(10_000, 20_000)]
    public int RecordsPerMinute { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _withoutLogLevelPolicyServices = CreateServices(retainProtectedLevels: false);
        _withLogLevelPolicyServices = CreateServices(retainProtectedLevels: true);
        _withoutLogLevelPolicyLoggers = CreateLoggers(_withoutLogLevelPolicyServices);
        _withLogLevelPolicyLoggers = CreateLoggers(_withLogLevelPolicyServices);
        _withoutLogLevelPolicyBuffer = _withoutLogLevelPolicyServices.GetRequiredService<LogBuffer>();
        _withLogLevelPolicyBuffer = _withLogLevelPolicyServices.GetRequiredService<LogBuffer>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _withLogLevelPolicyServices.Dispose();
        _withoutLogLevelPolicyServices.Dispose();
    }

    [IterationCleanup]
    public void FlushBuffers()
    {
        _withoutLogLevelPolicyBuffer.Flush();
        _withLogLevelPolicyBuffer.Flush();
    }

    [Benchmark(Baseline = true)]
    public void CckrWithoutLogLevelPolicy()
    {
        LoggingBenchmarkWorkload.LogBatch(_withoutLogLevelPolicyLoggers, RecordsPerMinute);
    }

    [Benchmark]
    public void CckrWithLogLevelPolicyMiss()
    {
        LoggingBenchmarkWorkload.LogBatch(_withLogLevelPolicyLoggers, RecordsPerMinute);
    }

    private static ServiceProvider CreateServices(bool retainProtectedLevels)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddProvider(new BenchLoggerProvider());
            builder.AddCckrLogSampling(options =>
            {
                options.Capacity = AdaptiveCapacity;
                options.PreserveCapacity = 0;
                options.FlushInterval = TimeSpan.FromDays(1);
                options.RetainAllLogLevels.Clear();

                if (retainProtectedLevels)
                {
                    options.RetainAllLogLevels.Add(LogLevel.Error);
                    options.RetainAllLogLevels.Add(LogLevel.Critical);
                }
            });
        });

        return services.BuildServiceProvider();
    }

    private static ILogger[] CreateLoggers(ServiceProvider services)
        => LoggingBenchmarkWorkload.CreateLoggers(services.GetRequiredService<ILoggerFactory>());
}
