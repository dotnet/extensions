// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    internal class ServiceScopeService : IService
    {
        public IService Next { get; set; }

        public LifecycleKind Lifecycle
        {
            get { return LifecycleKind.Scoped; }
        }

        public IServiceCallSite CreateCallSite(ServiceProvider provider)
        {
            return new CallSite();
        }

        private class CallSite : IServiceCallSite
        {
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
}
