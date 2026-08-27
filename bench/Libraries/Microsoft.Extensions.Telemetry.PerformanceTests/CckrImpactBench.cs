// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

[MemoryDiagnoser]
public class CckrImpactBench
{
    private const int AdaptiveCapacity = 128;
    private const int LogsPerMinute = 10_000;

    private static readonly Action<ILogger, int, Exception?> _logMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(1, "CckrBenchmark"),
            "CCKR benchmark message {Value}");

    private ServiceProvider _baselineServices = null!;
    private ServiceProvider _retainAllServices = null!;
    private ServiceProvider _adaptiveServices = null!;
    private ILogger _baselineLogger = null!;
    private ILogger _retainAllLogger = null!;
    private ILogger _adaptiveLogger = null!;
    private LogBuffer _retainAllBuffer = null!;
    private LogBuffer _adaptiveBuffer = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _baselineServices = CreateServices();
        _retainAllServices = CreateServices(LogsPerMinute);
        _adaptiveServices = CreateServices(AdaptiveCapacity);

        _baselineLogger = CreateLogger(_baselineServices);
        _retainAllLogger = CreateLogger(_retainAllServices);
        _adaptiveLogger = CreateLogger(_adaptiveServices);
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

    [Benchmark(Baseline = true, OperationsPerInvoke = LogsPerMinute)]
    public void NoSampling()
    {
        LogBatch(_baselineLogger);
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void CckrRetainAll()
    {
        LogBatch(_retainAllLogger);
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void CckrRetainAllAndFlush()
    {
        LogBatch(_retainAllLogger);
        _retainAllBuffer.Flush();
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void CckrAdaptive()
    {
        LogBatch(_adaptiveLogger);
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void CckrAdaptiveAndFlush()
    {
        LogBatch(_adaptiveLogger);
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

    private static ILogger CreateLogger(ServiceProvider services)
        => services.GetRequiredService<ILoggerFactory>().CreateLogger("CckrBenchmark");

    private static void LogBatch(ILogger logger)
    {
        for (int i = 0; i < LogsPerMinute; i++)
        {
            _logMessage(logger, i, null);
        }
    }
}
