// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Microsoft.Extensions.Telemetry.Bench;

internal static class Program
{
    public static void Main(string[] args)
    {
        var dontRequireSlnToRunBenchmarks = ManualConfig
            .Create(DefaultConfig.Instance)
            .AddJob(Job.MediumRun);

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, dontRequireSlnToRunBenchmarks);
    }
}
