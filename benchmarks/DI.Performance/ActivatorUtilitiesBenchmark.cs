// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Extensions.DependencyInjection.Performance
{
    public class ActivatorUtilitiesBenchmark
    {
        private ServiceProvider _serviceProvider;
        private ObjectFactory _factory;
        private object[] _factoryArguments;

        [GlobalSetup]
        public void SetUp()
        {
            var collection = new ServiceCollection();
            collection.AddTransient<TypeToBeActivated>();
            collection.AddSingleton<DependencyA>();
            collection.AddSingleton<DependencyB>();
            collection.AddSingleton<DependencyC>();
            collection.AddTransient<TypeToBeActivated>();

            _serviceProvider = collection.BuildServiceProvider();
            _factory = ActivatorUtilities.CreateFactory(typeof(TypeToBeActivated), new Type[] { typeof(DependencyB), typeof(DependencyC) });
            _factoryArguments = new object[] { new DependencyB(), new DependencyC() };
        }

        [Benchmark]
        public void ServiceProvider()
        {
           _serviceProvider.GetService<TypeToBeActivated>();
        }

        [Benchmark]
        public void Factory()
        {
            _ = (TypeToBeActivated)_factory(_serviceProvider, _factoryArguments);
        }

        [Benchmark]
        public void CreateInstance()
        {
            ActivatorUtilities.CreateInstance<TypeToBeActivated>(_serviceProvider, _factoryArguments);
        }

        public class TypeToBeActivated
        {
            public TypeToBeActivated(int i)
            {
                throw new NotImplementedException();
            }

            public TypeToBeActivated(string s)
            {
                throw new NotImplementedException();
            }

            public TypeToBeActivated(object o)
            {
                throw new NotImplementedException();
            }

            public TypeToBeActivated(DependencyA a, DependencyB b, DependencyC c)
            {
            }
        }

        public class DependencyA {}
        public class DependencyB {}
        public class DependencyC {}
    }
}
