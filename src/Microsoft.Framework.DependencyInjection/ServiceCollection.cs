// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceCollection : IServiceCollection
    {
        private readonly List<IServiceDescriptor> _descriptors;
        private readonly ServiceDescriber _describe;

        public ServiceCollection()
            : this(new Configuration())
        {
        }

        public ServiceCollection(IConfiguration configuration)
        {
            _descriptors = new List<IServiceDescriptor>();
            _describe = new ServiceDescriber(configuration);
        }

        public IServiceCollection Add(IServiceDescriptor descriptor)
        {
            _descriptors.Add(descriptor);
            return this;
        }

        public IServiceCollection Add(IEnumerable<IServiceDescriptor> descriptors)
        {
            _descriptors.AddRange(descriptors);
            return this;
        }

        public IServiceCollection AddTransient(Type service, Type implementationType)
        {
            Add(_describe.Transient(service, implementationType));
            return this;
        }

        public IServiceCollection AddTransient(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            return Add(_describe.Transient(service, implementationFactory));
        }

        public IServiceCollection AddScoped(Type service, Type implementationType)
        {
            Add(_describe.Scoped(service, implementationType));
            return this;
        }

        public IServiceCollection AddScoped(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            return Add(_describe.Scoped(service, implementationFactory));
        }

        public IServiceCollection AddSingleton(Type service, Type implementationType)
        {
            Add(_describe.Singleton(service, implementationType));
            return this;
        }

        public IServiceCollection AddInstance(Type service, object implementationInstance)
        {
            Add(_describe.Instance(service, implementationInstance));
            return this;
        }

        public IServiceCollection AddSingleton(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            return Add(_describe.Singleton(service, implementationFactory));
        }

        public IEnumerator<IServiceDescriptor> GetEnumerator()
        {
            return _descriptors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
