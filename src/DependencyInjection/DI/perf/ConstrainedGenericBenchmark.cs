// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class ConstrainedGenericBenchmark
    {
        private static ServiceDescriptor[] _noConstraintSds;
        private static ServiceDescriptor[] _newConstraintSds;
        private static ServiceDescriptor[] _notMatchingNewConstraintSds;
        private static ServiceDescriptor[] _simpleTypeConstraintSds;
        private static ServiceDescriptor[] _complexInterfaceConstraintSds;
        private static ServiceDescriptor[] _complexNotMatchingInterfaceConstraintSds;

        [GlobalSetup(Target = nameof(NoConstraints))]
        public void SetupNoConstraints()
        {
            _noConstraintSds = new[]
            {
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NoConstraintsOpenGenericService<>), ServiceLifetime.Transient),
            };
        }

        [Benchmark(Baseline = true)]
        public void NoConstraints()
        {
            var csf = new CallSiteFactory(_noConstraintSds);
            var _ = csf.GetCallSite(typeof(IEnumerable<IFakeGeneric<M>>), new CallSiteChain());
        }

        [GlobalSetup(Target = nameof(NewConstraint))]
        public void SetupNewConstraint()
        {
            _newConstraintSds = new[]
            {
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
            };
        }

        [Benchmark]
        public void NewConstraint()
        {
            var csf = new CallSiteFactory(_newConstraintSds);
            var _ = csf.GetCallSite(typeof(IEnumerable<IFakeGeneric<K>>), new CallSiteChain());
        }

        [GlobalSetup(Target = nameof(NotMatchingNewConstraint))]
        public void SetupNotMatchingNewConstraint()
        {
            _notMatchingNewConstraintSds = new[]
            {
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>), ServiceLifetime.Transient),
            };
        }

        [Benchmark]
        public void NotMatchingNewConstraint()
        {
            var csf = new CallSiteFactory(_notMatchingNewConstraintSds);
            var _ = csf.GetCallSite(typeof(IEnumerable<IFakeGeneric<L>>), new CallSiteChain());
        }

        [GlobalSetup(Target = nameof(SimpleTypeConstraint))]
        public void SetupSimpleTypeConstraint()
        {
            _simpleTypeConstraintSds = new[]
            {
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>), ServiceLifetime.Transient),
            };
        }

        [Benchmark]
        public void SimpleTypeConstraint()
        {
            var csf = new CallSiteFactory(_simpleTypeConstraintSds);
            var _ = csf.GetCallSite(typeof(IEnumerable<IFakeGeneric<K>>), new CallSiteChain());
        }

        [GlobalSetup(Target = nameof(ComplexInterfaceConstraint))]
        public void SetupComplexInterfaceConstraint()
        {
            _complexInterfaceConstraintSds = new[]
            {
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
            };
        }

        [Benchmark]
        public void ComplexInterfaceConstraint()
        {
            var csf = new CallSiteFactory(_complexInterfaceConstraintSds);
            var _ = csf.GetCallSite(typeof(IEnumerable<IFakeGeneric<N>>), new CallSiteChain());
        }

        [GlobalSetup(Target = nameof(NotMatchingComplexInterfaceConstraint))]
        public void SetupNotMatchingComplexInterfaceConstraint()
        {
            _complexNotMatchingInterfaceConstraintSds = new[]
            {
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>), ServiceLifetime.Transient),
            };
        }

        [Benchmark]
        public void NotMatchingComplexInterfaceConstraint()
        {
            var csf = new CallSiteFactory(_complexNotMatchingInterfaceConstraintSds);
            var _ = csf.GetCallSite(typeof(IEnumerable<IFakeGeneric<N[]>>), new CallSiteChain());
        }

        private interface IFakeGeneric<T>
        {
            void Foo();
        }

        private class NoConstraintsOpenGenericService<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class NewConstraintOpenGenericService<T> : IFakeGeneric<T>
            where T : new()
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class AbstractClassOpenGenericService<T> : IFakeGeneric<T>
            where T : I
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class SelfReferencingOpenGenericService<T> : IFakeGeneric<T>
            where T : IComparable<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class I { }
        private class J : I { }
        private class K : J { }

        private class L
        {
            private L() { }
        }

        private class M { }

        private class N : IComparable<N>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public int CompareTo(N other)
            {
                return 0;
            }
        }
    }
}
