// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class ConstrainedGenericBenchmark
    {
        private const int OperationsPerInvoke = 50000;

        private IServiceProvider _noConstraintsSp;
        private IServiceProvider _newConstraintSp;
        private IServiceProvider _notMatchingNewConstraintSp;
        private IServiceProvider _simpleTypeConstraintSp;
        private IServiceProvider _complexInterfaceConstraintSp;
        private IServiceProvider _complexNotMatchingInterfaceConstraintSp;
        private ServiceProviderMode _mode;

        [Params("Expressions", "Dynamic", "Runtime", "ILEmit")]
        public string Mode {
            set {
                _mode = (ServiceProviderMode)Enum.Parse(typeof(ServiceProviderMode), value);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
        public void NoDI()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = new IFakeGeneric<J>[]
                {
                    new A<J>(), 
                    new B<J>(), 
                    new C<J>(), 
                    new D<J>(), 
                    new E<J>(), 
                    new F<J>(), 
                    new G<J>(), 
                    new H<J>()
                };
                temp[0].Foo();
            }
        }

        [GlobalSetup(Target = nameof(NoConstraints))]
        public void SetupNoConstraints()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IFakeGeneric<>), typeof(A<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(B<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(C<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(D<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(E<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(F<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(G<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(H<>));
            _noConstraintsSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void NoConstraints()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _noConstraintsSp.GetServices<IFakeGeneric<M>>();
                temp.ToList()[0].Foo();
            }
        }

        [GlobalSetup(Target = nameof(NewConstraint))]
        public void SetupNewConstraint()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IFakeGeneric<>), typeof(A<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(B<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(C<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(D<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(E<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(F<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(G<>));
            _newConstraintSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void NewConstraint()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _newConstraintSp.GetServices<IFakeGeneric<K>>();
                temp.ToList()[0].Foo();
            }
        }

        [GlobalSetup(Target = nameof(NotMatchingNewConstraint))]
        public void SetupNotMatchingNewConstraint()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IFakeGeneric<>), typeof(A<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(B<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(C<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(D<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(E<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(F<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(NewConstraintOpenGenericService<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(G<>));
            _notMatchingNewConstraintSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void NotMatchingNewConstraint()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _notMatchingNewConstraintSp.GetServices<IFakeGeneric<L>>();
                temp.ToList()[0].Foo();
            }
        }

        [GlobalSetup(Target = nameof(SimpleTypeConstraint))]
        public void SetupSimpleTypeConstraint()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IFakeGeneric<>), typeof(A<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(B<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(C<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(D<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(E<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(F<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(AbstractClassOpenGenericService<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(G<>));
            _simpleTypeConstraintSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void SimpleTypeConstraint()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _simpleTypeConstraintSp.GetServices<IFakeGeneric<K>>();
                temp.ToList()[0].Foo();
            }
        }

        [GlobalSetup(Target = nameof(ComplexInterfaceConstraint))]
        public void SetupComplexInterfaceConstraint()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IFakeGeneric<>), typeof(A<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(B<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(C<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(D<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(E<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(F<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(G<>));
            _complexInterfaceConstraintSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void ComplexInterfaceConstraint()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _complexInterfaceConstraintSp.GetServices<IFakeGeneric<N>>();
                temp.ToList()[0].Foo();
            }
        }

        [GlobalSetup(Target = nameof(NotMatchingComplexInterfaceConstraint))]
        public void SetupNotMatchingComplexInterfaceConstraint()
        {
            var services = new ServiceCollection();
            services.AddTransient(typeof(IFakeGeneric<>), typeof(A<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(B<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(C<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(D<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(E<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(F<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(SelfReferencingOpenGenericService<>));
            services.AddTransient(typeof(IFakeGeneric<>), typeof(G<>));
            _complexNotMatchingInterfaceConstraintSp = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = _mode
            });
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void NotMatchingComplexInterfaceConstraint()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var temp = _complexNotMatchingInterfaceConstraintSp.GetServices<IFakeGeneric<N[]>>();
                temp.ToList()[0].Foo();
            }
        }

        private interface IFakeGeneric<T>
        {
            void Foo();
        }

        private class A<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class B<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class C<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class D<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class E<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class F<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class G<T> : IFakeGeneric<T>
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Foo()
            {

            }
        }

        private class H<T> : IFakeGeneric<T>
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
