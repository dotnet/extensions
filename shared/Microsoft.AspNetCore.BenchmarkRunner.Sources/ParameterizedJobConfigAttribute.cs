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

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class ParameterizedJobConfigAttribute : Attribute, IConfigSource
    {
        public static Job Job { get; set; }

        public ParameterizedJobConfigAttribute(Type config)
        {
            var args = Job != null ? new object[] { Job } : Array.Empty<object>();
            Config = (IConfig) Activator.CreateInstance(config, args);
        }

        public IConfig Config { get; }
    }
}