// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class FactoryService : IService, IServiceCallSite
    {
        private readonly ServiceDescriptor _descriptor;

        public FactoryService(ServiceDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public IService Next { get; set; }

        public ServiceLifetime Lifetime
        {
            get { return _descriptor.Lifetime; }
        }

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            return this;
        }

        public object Invoke(ServiceProvider provider)
        {
            return _descriptor.ImplementationFactory(provider);
        }

        public Expression Build(Expression provider)
        {
            Expression<Func<IServiceProvider, object>> factory =
                serviceProvider => _descriptor.ImplementationFactory(serviceProvider);

            return Expression.Invoke(factory, provider);
        }
    }
}
