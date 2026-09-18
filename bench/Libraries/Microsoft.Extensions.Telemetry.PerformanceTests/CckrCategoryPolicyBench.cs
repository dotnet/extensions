// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

/// <summary>
/// Measures the incremental cost of evaluating CCKR retain-all category policies.
/// </summary>
[MemoryDiagnoser]
[InvocationCount(1)]
public class CckrCategoryPolicyBench
{
    private const int AdaptiveCapacity = 128;

    private ServiceProvider _withoutCategoryPolicyServices = null!;
    private ServiceProvider _withCategoryPolicyServices = null!;
    private ILogger[] _withoutCategoryPolicyLoggers = null!;
    private ILogger[] _withCategoryPolicyLoggers = null!;
    private LogBuffer _withoutCategoryPolicyBuffer = null!;
    private LogBuffer _withCategoryPolicyBuffer = null!;

    [Params(10_000, 20_000)]
    public int RecordsPerMinute { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _withoutCategoryPolicyServices = CreateServices(retainProtectedCategories: false);
        _withCategoryPolicyServices = CreateServices(retainProtectedCategories: true);
        _withoutCategoryPolicyLoggers = CreateLoggers(_withoutCategoryPolicyServices);
        _withCategoryPolicyLoggers = CreateLoggers(_withCategoryPolicyServices);
        _withoutCategoryPolicyBuffer = _withoutCategoryPolicyServices.GetRequiredService<LogBuffer>();
        _withCategoryPolicyBuffer = _withCategoryPolicyServices.GetRequiredService<LogBuffer>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _withCategoryPolicyServices.Dispose();
        _withoutCategoryPolicyServices.Dispose();
    }

    [IterationCleanup]
    public void FlushBuffers()
    {
        _withoutCategoryPolicyBuffer.Flush();
        _withCategoryPolicyBuffer.Flush();
    }

    [Benchmark(Baseline = true)]
    public void CckrWithoutCategoryPolicy()
    {
        LoggingBenchmarkWorkload.LogBatch(_withoutCategoryPolicyLoggers, RecordsPerMinute);
    }

    [Benchmark]
    public void CckrWithCategoryPolicyMiss()
    {
        LoggingBenchmarkWorkload.LogBatch(_withCategoryPolicyLoggers, RecordsPerMinute);
    }

    private static ServiceProvider CreateServices(bool retainProtectedCategories)
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

                if (retainProtectedCategories)
                {
                    options.RetainAllCategories.Add("Contoso.Security.*");
                    options.RetainAllCategories.Add("Contoso.Audit.*");
                }
            });
        });

        return services.BuildServiceProvider();
    }

    private static ILogger[] CreateLoggers(ServiceProvider services)
        => LoggingBenchmarkWorkload.CreateLoggers(services.GetRequiredService<ILoggerFactory>());
}
