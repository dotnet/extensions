// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class ServiceProviderEngineBenchmark
    {
        internal ServiceProviderMode ServiceProviderMode { get; private set; }

        [Params("Expressions", "Dynamic", "Runtime", "ILEmit")]
        public string Mode {
            set {
                ServiceProviderMode = (ServiceProviderMode)Enum.Parse(typeof(ServiceProviderMode), value);
            }
        }

        internal class A
        {
            public A(B b)
            {

            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        internal class B
        {
            public B(C c)
            {

            }
        }

        internal class C
        {

        }

    }
}
