// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Polly;
using Polly.Registry;
using Polly.Telemetry;

namespace Microsoft.Extensions.Resilience.Bench;

public class ResilienceEnrichmentBenchmark
{
    private MeterListener? _listener;
    private ResiliencePipeline? _pipeline;
    private ResiliencePipeline? _pipelineEnriched;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _listener = MetricsUtil.ListenPollyMetrics();
        _pipeline = CreateResiliencePipeline(_ => { });
        _pipelineEnriched = CreateResiliencePipeline(services =>
        {
            services.AddResilienceEnrichment();
            services.ConfigureFailureResultContext<string>(res => FailureResultContext.Create("dummy", "dummy", "dummy"));
        });
    }

    [GlobalCleanup]
    public void Cleanup() => _listener?.Dispose();

    [Benchmark(Baseline = true)]
    public void ReportTelemetry() => _pipeline!.Execute(() => "dummy-result");

    [Benchmark]
    public void ReportTelemetry_Enriched() => _pipelineEnriched!.Execute(() => "dummy-result");

    private static ResiliencePipeline CreateResiliencePipeline(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExceptionSummarizer();
        services.AddResiliencePipeline("my-pipeline", builder => builder.AddStrategy(context => new DummyStrategy(context.Telemetry), new DummyOptions()));
        services.AddLogging();
        configure(services);

        return services.BuildServiceProvider().GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline("my-pipeline");
    }

    private class DummyStrategy : ResilienceStrategy
    {
        private readonly ResilienceStrategyTelemetry _telemetry;

        public DummyStrategy(ResilienceStrategyTelemetry telemetry)
        {
            _telemetry = telemetry;
        }

        protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            _telemetry.Report(new ResilienceEvent(ResilienceEventSeverity.Information, "Dummy"), context, "dummy-args");

            return callback(context, state);
        }
    }

    private class DummyOptions : ResilienceStrategyOptions
    {
    }
}
