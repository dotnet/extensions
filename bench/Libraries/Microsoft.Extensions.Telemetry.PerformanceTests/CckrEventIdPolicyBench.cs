// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

/// <summary>
/// Measures the incremental cost of evaluating CCKR retain-all EventId policies.
/// </summary>
[MemoryDiagnoser]
[InvocationCount(1)]
public class CckrEventIdPolicyBench
{
    private const int AdaptiveCapacity = 128;

    private ServiceProvider _withoutEventIdPolicyServices = null!;
    private ServiceProvider _withEventIdPolicyServices = null!;
    private ILogger[] _withoutEventIdPolicyLoggers = null!;
    private ILogger[] _withEventIdPolicyLoggers = null!;
    private LogBuffer _withoutEventIdPolicyBuffer = null!;
    private LogBuffer _withEventIdPolicyBuffer = null!;

    [Params(10_000, 20_000)]
    public int RecordsPerMinute { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _withoutEventIdPolicyServices = CreateServices(retainProtectedEventIds: false);
        _withEventIdPolicyServices = CreateServices(retainProtectedEventIds: true);
        _withoutEventIdPolicyLoggers = CreateLoggers(_withoutEventIdPolicyServices);
        _withEventIdPolicyLoggers = CreateLoggers(_withEventIdPolicyServices);
        _withoutEventIdPolicyBuffer = _withoutEventIdPolicyServices.GetRequiredService<LogBuffer>();
        _withEventIdPolicyBuffer = _withEventIdPolicyServices.GetRequiredService<LogBuffer>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _withEventIdPolicyServices.Dispose();
        _withoutEventIdPolicyServices.Dispose();
    }

    [IterationCleanup]
    public void FlushBuffers()
    {
        _withoutEventIdPolicyBuffer.Flush();
        _withEventIdPolicyBuffer.Flush();
    }

    [Benchmark(Baseline = true)]
    public void CckrWithoutEventIdPolicy()
    {
        LoggingBenchmarkWorkload.LogBatch(_withoutEventIdPolicyLoggers, RecordsPerMinute);
    }

    [Benchmark]
    public void CckrWithEventIdPolicyMiss()
    {
        LoggingBenchmarkWorkload.LogBatch(_withEventIdPolicyLoggers, RecordsPerMinute);
    }

    private static ServiceProvider CreateServices(bool retainProtectedEventIds)
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

                if (retainProtectedEventIds)
                {
                    options.RetainAllEventIds.Add(10_001);
                    options.RetainAllEventIds.Add(10_002);
                }
            });
        });

        return services.BuildServiceProvider();
    }

    private static ILogger[] CreateLoggers(ServiceProvider services)
        => LoggingBenchmarkWorkload.CreateLoggers(services.GetRequiredService<ILoggerFactory>());
}
