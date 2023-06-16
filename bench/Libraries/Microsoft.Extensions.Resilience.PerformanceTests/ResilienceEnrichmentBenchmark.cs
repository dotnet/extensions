// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    private ResilienceStrategy? _strategy;
    private ResilienceStrategy? _strategyEnriched;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _strategy = CreateResilienceStrategy(_ => { });
        _strategyEnriched = CreateResilienceStrategy(services =>
        {
            services.AddResilienceEnrichment();
            services.ConfigureFailureResultContext<string>(res => FailureResultContext.Create("dummy", "dummy", "dummy"));
        });
    }


    [Benchmark(Baseline = true)]
    public void ReportTelemetry() => _strategy!.Execute(() => "dummy-result");

    [Benchmark]
    public void ReportTelemetry_Enriched() => _strategyEnriched!.Execute(() => "dummy-result");

    private static ResilienceStrategy CreateResilienceStrategy(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExceptionSummarizer();
        services.AddResilienceStrategy("my-strategy", builder => builder.AddStrategy(context => new DummyStrategy(context.Telemetry), new DummyOptions()));
        services.AddLogging();
        configure(services);

        return services.BuildServiceProvider().GetRequiredService<ResilienceStrategyProvider<string>>().Get("my-strategy");
    }

    private class DummyStrategy : ResilienceStrategy
    {
        private readonly ResilienceStrategyTelemetry _telemetry;

        public DummyStrategy(ResilienceStrategyTelemetry telemetry)
        {
            _telemetry = telemetry;
        }

        protected override ValueTask<Outcome<TResult>> ExecuteCoreAsync<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            _telemetry.Report("Dummy", context, "dummy-args");

            return callback(context, state);
        }
    }

    private class DummyOptions : ResilienceStrategyOptions
    {
        public override string StrategyType => "Dummy";
    }
}
