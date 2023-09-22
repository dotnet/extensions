// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Microsoft.Extensions.Http.Logging.Bench;

internal static class Program
{
    private static void Main(string[] args)
    {
        var dontRequireSlnToRunBenchmarks = ManualConfig
            .Create(DefaultConfig.Instance)
            .AddJob(Job.MediumRun
                .WithRuntime(CoreRuntime.Core50)
                .WithGcServer(true)
                .WithJit(Jit.RyuJit)
                .WithPlatform(Platform.X64)
                .WithToolchain(InProcessEmitToolchain.Instance))
                .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared))
            .AddDiagnoser(MemoryDiagnoser.Default);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, dontRequireSlnToRunBenchmarks);
    }
}
