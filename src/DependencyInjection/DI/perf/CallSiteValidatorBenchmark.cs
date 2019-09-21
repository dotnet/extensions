// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class CallSiteValidatorBenchmark
    {
        private ServiceCallSite _callSite;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddTransient<A>();
            services.AddTransient<B>();
            services.AddTransient<C>();
            services.AddTransient<D>();
            services.AddTransient<E>();
            services.AddTransient<F>();
            services.AddTransient<G>();
            services.AddTransient<H>();
            services.AddTransient<I>();
            services.AddTransient<J>();
            services.AddTransient<K>();
            services.AddTransient<L>();
            services.AddTransient<M>();
            services.AddTransient<N>();
            services.AddTransient<O>();
            services.AddTransient<P>();

            var callSiteFactory = new CallSiteFactory(services.ToArray());

            _callSite = callSiteFactory.GetCallSite(typeof(A), new CallSiteChain());
        }

        [Benchmark()]
        public void ValidateCallSite()
        {
            var callSiteValidator = new CallSiteValidator();

            callSiteValidator.ValidateCallSite(_callSite);
        }

        private class A
        {
            public A(B b, C c, D d, E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class B
        {
            public B(C c, D d, E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class C
        {
            public C(D d, E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }

        }

        private class D
        {
            public D(E e, F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class E
        {
            public E(F f, G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class F
        {
            public F(G g, H h, I i, J j, K k, L l)
            {

            }
        }

        private class G
        {
            public G(H h, I i, J j, K k, L l)
            {

            }
        }

        private class H
        {
            public H(I i, J j, K k, L l)
            {

            }
        }

        private class I
        {
            public I(J j, K k, L l)
            {

            }
        }

        private class J
        {
            public J(K k, L l)
            {

            }
        }

        private class K
        {
            public K(L l)
            {

            }
        }

        private class L
        {
            public L(M m)
            {

            }
        }

        private class M
        {
            public M(N n)
            {

            }
        }

        private class N
        {
            public N(O o)
            {

            }
        }

        private class O
        {
            public O(P p)
            {

            }
        }

        private class P
        {
            public P()
            {

            }
        }

    }
}
