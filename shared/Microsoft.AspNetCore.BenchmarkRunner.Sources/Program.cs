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
        static void Main(string[] args)
        {
            CheckValidate(ref args);
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly)
                .Run(args, ManualConfig.CreateEmpty());
        }

        static void CheckValidate(ref string[] args)
        {
            var argsList = args.ToList();
            if (argsList.Remove("--validate"))
            {
                ConditionalJobConfig.Job = Job.Dry;
            }

            if (argsList.Remove("--validate-fast"))
            {
                ConditionalJobConfig.Job = Job.Dry.With(InProcessToolchain.Instance);
            }

            args = argsList.ToArray();
        }
    }
}