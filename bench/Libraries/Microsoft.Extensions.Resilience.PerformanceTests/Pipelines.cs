// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Polly;
using Polly.NoOp;

namespace Microsoft.Extensions.Resilience.Bench;

public class Pipelines
{
    private static readonly Task<string> _completed = Task.FromResult("dummy");
    private IAsyncPolicy<string>? _bulkheadPolicy;
    private IAsyncPolicy<string>? _timeoutPolicy;
    private IAsyncPolicy<string>? _circuitBreaker;
    private IAsyncPolicy<string>? _retryPolicy;
    private IAsyncPolicy<string>? _fallbackPolicy;
    private IAsyncPolicy<string>? _hedgingPolicy;
    private AsyncNoOpPolicy<string>? _noOp;
    private CancellationToken _cancellation;
    private Context? _context;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterMetering();
        services.AddResiliencePipeline<string>(nameof(SupportedPolicies.BulkheadPolicy)).AddBulkheadPolicy("dummy");
        services.AddResiliencePipeline<string>(nameof(SupportedPolicies.TimeoutPolicy)).AddTimeoutPolicy("dummy");
        services.AddResiliencePipeline<string>(nameof(SupportedPolicies.CircuitBreaker)).AddCircuitBreakerPolicy("dummy");
        services.AddResiliencePipeline<string>(nameof(SupportedPolicies.RetryPolicy)).AddRetryPolicy("dummy");
        services.AddResiliencePipeline<string>(nameof(SupportedPolicies.FallbackPolicy)).AddFallbackPolicy("dummy", task => _completed);
        services.AddResiliencePipeline<string>(nameof(SupportedPolicies.HedgingPolicy)).AddHedgingPolicy("dummy", (HedgingTaskProviderArguments _, out Task<string>? result) =>
        {
            result = null;
            return false;
        });

        var pipelineProvider = services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>();
        _context = new Context();
        _bulkheadPolicy = pipelineProvider.GetPipeline<string>(nameof(SupportedPolicies.BulkheadPolicy));
        _timeoutPolicy = pipelineProvider.GetPipeline<string>(nameof(SupportedPolicies.TimeoutPolicy));
        _circuitBreaker = pipelineProvider.GetPipeline<string>(nameof(SupportedPolicies.CircuitBreaker));
        _retryPolicy = pipelineProvider.GetPipeline<string>(nameof(SupportedPolicies.RetryPolicy));
        _fallbackPolicy = pipelineProvider.GetPipeline<string>(nameof(SupportedPolicies.FallbackPolicy));
        _hedgingPolicy = pipelineProvider.GetPipeline<string>(nameof(SupportedPolicies.HedgingPolicy));
        _noOp = Policy.NoOpAsync<string>();
        _cancellation = new CancellationTokenSource().Token;
    }

    [Benchmark(Baseline = true)]
    public Task NoOp() => _noOp!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);

    [Benchmark]
    public Task Bulkhead() => _bulkheadPolicy!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);

    [Benchmark]
    public Task Timeout() => _timeoutPolicy!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);

    [Benchmark]
    public Task CircuitBreaker() => _circuitBreaker!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);

    [Benchmark]
    public Task Retry() => _retryPolicy!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);

    [Benchmark]
    public Task Fallback() => _fallbackPolicy!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);

    [Benchmark]
    public Task Hedging() => _hedgingPolicy!.ExecuteAsync(static (_, _) => _completed, _context, _cancellation);
}
