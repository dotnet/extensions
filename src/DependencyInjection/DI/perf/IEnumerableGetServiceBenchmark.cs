// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class IEnumerableGetServiceBenchmark: ServiceProviderEngineBenchmark
    {
        private const int OperationsPerInvoke = 50000;

        private IServiceProvider _serviceProvider;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Transient()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _serviceProvider.GetService<IEnumerable<A>>();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Scoped()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _serviceProvider.GetService<IEnumerable<A>>();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Singleton()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _serviceProvider.GetService<IEnumerable<A>>();
            }
        }

        [GlobalSetup(Target = nameof(Transient))]
        public void SetupTransient() => Setup(ServiceLifetime.Transient);

        [GlobalSetup(Target = nameof(Scoped))]
        public void SetupScoped() => Setup(ServiceLifetime.Scoped);

        [GlobalSetup(Target = nameof(Singleton))]
        public void SetupSingleton() => Setup(ServiceLifetime.Singleton);

        private void Setup(ServiceLifetime lifetime)
        {
            IServiceCollection services = new ServiceCollection();
            for (int i = 0; i < 10; i++)
            {
                services.Add(ServiceDescriptor.Describe(typeof(A), typeof(A), lifetime));
            }

            services.Add(ServiceDescriptor.Describe(typeof(B), typeof(B), lifetime));
            services.Add(ServiceDescriptor.Describe(typeof(C), typeof(C), lifetime));

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions()
            {
                Mode = ServiceProviderMode
            }).CreateScope().ServiceProvider;
        }
    }
}
