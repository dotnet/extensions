// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess;

namespace Microsoft.AspNetCore.BenchmarkDotNet.Runner
{
    class Program
    {
        static int Main(string[] args)
        {
            CheckValidate(ref args);
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly)
                .Run(args, ManualConfig.CreateEmpty());

            foreach (var summary in summaries)
            {
                if (summary.HasCriticalValidationErrors)
                {
                    return Fail(summary, nameof(summary.HasCriticalValidationErrors));
                }

                foreach (var report in summary.Reports)
                {
                    if (!report.BuildResult.IsGenerateSuccess)
                    {
                        return Fail(report, nameof(report.BuildResult.IsGenerateSuccess));
                    }

                    if (!report.BuildResult.IsBuildSuccess)
                    {
                        return Fail(report, nameof(report.BuildResult.IsBuildSuccess));
                    }

                    if (!report.AllMeasurements.Any())
                    {
                        return Fail(report, nameof(report.AllMeasurements));
                    }
                }
            }

            return 0;
        }

        static int Fail(object o, string message)
        {
            Console.WriteLine("'{0}' failed, reason: '{1}'", o, message);
            return 1;
        }

        static void CheckValidate(ref string[] args)
        {
            var argsList = args.ToList();
            if (argsList.Remove("--validate"))
            {
                ParameterizedJobConfigAttribute.Job = Job.Dry;
            }

            if (argsList.Remove("--validate-fast"))
            {
                ParameterizedJobConfigAttribute.Job = Job.Dry.With(InProcessToolchain.Instance);
            }

            args = argsList.ToArray();
        }
    }
}