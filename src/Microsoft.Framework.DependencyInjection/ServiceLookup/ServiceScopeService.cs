// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    internal class ServiceScopeService : IService, IServiceCallSite
    {
        public IService Next { get; set; }

        public LifecycleKind Lifecycle
        {
            get { return LifecycleKind.Scoped; }
        }

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            return this;
        }

        public object Invoke(ServiceProvider provider)
        {
            return new ServiceScopeFactory(provider);
        }

        public Expression Build(Expression provider)
        {
            return Expression.New(
                typeof(ServiceScopeFactory).GetTypeInfo()
                    .DeclaredConstructors
                    .Single(),
                provider);
        }
    }
}
