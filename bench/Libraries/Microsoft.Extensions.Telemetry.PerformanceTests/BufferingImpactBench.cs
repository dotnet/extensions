// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

[MemoryDiagnoser]
public class BufferingImpactBench
{
    private const int LogsPerMinute = 10_000;

    private static readonly Action<ILogger, int, Exception?> _logMessage =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(1, "BufferingBenchmark"),
            "Buffering benchmark message {Value}");

    private ServiceProvider _baselineServices = null!;
    private ServiceProvider _bufferedServices = null!;
    private ILogger _baselineLogger = null!;
    private ILogger _bufferedLogger = null!;
    private GlobalLogBuffer _buffer = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _baselineServices = CreateServices(bufferingEnabled: false);
        _bufferedServices = CreateServices(bufferingEnabled: true);
        _baselineLogger = _baselineServices.GetRequiredService<ILoggerFactory>().CreateLogger("BufferingBenchmark");
        _bufferedLogger = _bufferedServices.GetRequiredService<ILoggerFactory>().CreateLogger("BufferingBenchmark");
        _buffer = _bufferedServices.GetRequiredService<GlobalLogBuffer>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _bufferedServices.Dispose();
        _baselineServices.Dispose();
    }

    [IterationCleanup]
    public void FlushBuffer()
    {
        _buffer.Flush();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = LogsPerMinute)]
    public void NoBuffering()
    {
        LogBatch(_baselineLogger);
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void BufferOnly()
    {
        LogBatch(_bufferedLogger);
    }

    [Benchmark(OperationsPerInvoke = LogsPerMinute)]
    public void BufferAndFlush()
    {
        LogBatch(_bufferedLogger);
        _buffer.Flush();
    }

    private static ServiceProvider CreateServices(bool bufferingEnabled)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddProvider(new BenchLoggerProvider());

            if (bufferingEnabled)
            {
                builder.AddGlobalBuffer(options =>
                {
                    options.AutoFlushDuration = TimeSpan.Zero;
                    options.MaxBufferSizeInBytes = 512 * 1024 * 1024;
                    options.Rules.Add(new LogBufferingFilterRule(logLevel: LogLevel.Information));
                });
            }
        });

        return services.BuildServiceProvider();
    }

    private static void LogBatch(ILogger logger)
    {
        for (int i = 0; i < LogsPerMinute; i++)
        {
            _logMessage(logger, i, null);
        }
    }
}
