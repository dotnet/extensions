// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Microsoft.Extensions.Telemetry.Bench;

internal static class Program
{
    public static void Main(string[] args)
    {
#if false
        var lf = new LoggerFactory();
        lf.Factory = LoggerFactory.LoggerFactoryVersions.Original;
        lf.ModernCodeGen_RefTypes();
#endif

        var dontRequireSlnToRunBenchmarks = ManualConfig
            .Create(DefaultConfig.Instance)
            .AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, dontRequireSlnToRunBenchmarks);
    }
}
