// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Resilience.Bench;

public class PipelineProvider
{
    private IResiliencePipelineProvider? _pipelineProvider;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.RegisterMetering();
        services.AddResiliencePipeline<string>("dummy").AddBulkheadPolicy("dummy");

        _pipelineProvider = services.BuildServiceProvider().GetRequiredService<IResiliencePipelineProvider>();
    }

    [Benchmark(Baseline = true)]
    public void GetPipeline() => _pipelineProvider!.GetPipeline<string>("dummy");

    [Benchmark]
    public void GetPipelineByKey() => _pipelineProvider!.GetPipeline<string>("dummy", "dummy-key");
}
