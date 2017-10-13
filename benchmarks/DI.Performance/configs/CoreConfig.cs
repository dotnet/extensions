// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class CoreConfig : ManualConfig
    {
        public CoreConfig():
            this(Job.Core
                    .WithRemoveOutliers(false)
                    .With(RunStrategy.Throughput))
        {
            Add(JitOptimizationsValidator.FailOnError);
        }

        public CoreConfig(Job job)
        {
            Add(DefaultConfig.Instance);

            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.OperationsPerSecond);

            Add(job);
        }
    }
}
