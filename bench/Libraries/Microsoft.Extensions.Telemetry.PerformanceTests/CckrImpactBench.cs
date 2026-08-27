// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

[MemoryDiagnoser]
[InvocationCount(1)]
public class CckrImpactBench
{
    private const int AdaptiveCapacity = 128;

    private ServiceProvider _baselineServices = null!;
    private ServiceProvider _retainAllServices = null!;
    private ServiceProvider _adaptiveServices = null!;
    private ILogger[] _baselineLoggers = null!;
    private ILogger[] _retainAllLoggers = null!;
    private ILogger[] _adaptiveLoggers = null!;
    private LogBuffer _retainAllBuffer = null!;
    private LogBuffer _adaptiveBuffer = null!;

    [Params(10_000, 20_000)]
    public int RecordsPerMinute { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _baselineServices = CreateServices();
        _retainAllServices = CreateServices(RecordsPerMinute);
        _adaptiveServices = CreateServices(AdaptiveCapacity);

        _baselineLoggers = CreateLoggers(_baselineServices);
        _retainAllLoggers = CreateLoggers(_retainAllServices);
        _adaptiveLoggers = CreateLoggers(_adaptiveServices);
        _retainAllBuffer = _retainAllServices.GetRequiredService<LogBuffer>();
        _adaptiveBuffer = _adaptiveServices.GetRequiredService<LogBuffer>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _adaptiveServices.Dispose();
        _retainAllServices.Dispose();
        _baselineServices.Dispose();
    }

    [IterationCleanup]
    public void FlushBuffers()
    {
        _retainAllBuffer.Flush();
        _adaptiveBuffer.Flush();
    }

    [Benchmark(Baseline = true)]
    public void NoSampling()
    {
        LoggingBenchmarkWorkload.LogBatch(_baselineLoggers, RecordsPerMinute);
    }

    [Benchmark]
    public void CckrRetainAll()
    {
        LoggingBenchmarkWorkload.LogBatch(_retainAllLoggers, RecordsPerMinute);
    }

    [Benchmark]
    public void CckrRetainAllAndFlush()
    {
        LoggingBenchmarkWorkload.LogBatch(_retainAllLoggers, RecordsPerMinute);
        _retainAllBuffer.Flush();
    }

    [Benchmark]
    public void CckrAdaptive()
    {
        LoggingBenchmarkWorkload.LogBatch(_adaptiveLoggers, RecordsPerMinute);
    }

    [Benchmark]
    public void CckrAdaptiveAndFlush()
    {
        LoggingBenchmarkWorkload.LogBatch(_adaptiveLoggers, RecordsPerMinute);
        _adaptiveBuffer.Flush();
    }

    private static ServiceProvider CreateServices(int? capacity = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddProvider(new BenchLoggerProvider());

            if (capacity.HasValue)
            {
                builder.AddCckrLogSampling(options =>
                {
                    options.Capacity = capacity.Value;
                    options.PreserveCapacity = 0;
                    options.FlushInterval = TimeSpan.FromDays(1);
                });
            }
        });

        return services.BuildServiceProvider();
    }

    private static ILogger[] CreateLoggers(ServiceProvider services)
        => LoggingBenchmarkWorkload.CreateLoggers(services.GetRequiredService<ILoggerFactory>());
}
