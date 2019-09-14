// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop.Performance
{
    public class DotNetDispatcherBenchmark
    {
        private JSRuntime jsRuntime;
        private string assemblyName;
        private string argsJson;

        [GlobalSetup]
        public void SetUp()
        {
            jsRuntime = new TestJSRuntime();
            assemblyName = "Microsoft.JSInterop.Performance";
            argsJson = "[1, \"test\"]";
        }

        [Benchmark]
        public void Invoke()
        {
            DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(assemblyName, nameof(TestType.InvocableStaticVoid), default, default), argsJson);
        }

        public class TestType
        {
            [JSInvokable]
            public static void InvocableStaticVoid(int a, string b)
            {
            }
        }

        public class TestJSRuntime : JSRuntime
        {
            protected override void BeginInvokeJS(long taskId, string identifier, string argsJson)
            {
            }

            protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            {
            }
        }
    }
}
