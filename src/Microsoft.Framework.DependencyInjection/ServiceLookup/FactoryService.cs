// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    internal class FactoryService : IService, IServiceCallSite
    {
        private readonly IServiceDescriptor _descriptor;

        public FactoryService(IServiceDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public IService Next { get; set; }

        public LifecycleKind Lifecycle
        {
            get { return _descriptor.Lifecycle; }
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
