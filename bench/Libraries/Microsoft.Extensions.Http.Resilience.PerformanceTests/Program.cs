﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Benchmark
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var doNotRequireSlnToRunBenchmarks = ManualConfig
                .Create(DefaultConfig.Instance)
                .AddJob(Job.MediumRun.WithToolchain(InProcessEmitToolchain.Instance))
                .AddDiagnoser(MemoryDiagnoser.Default);

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, doNotRequireSlnToRunBenchmarks);
        }
    }
}
