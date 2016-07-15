// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceScopeService : IService, IServiceCallSite
    {
        public IService Next { get; set; }

        public ServiceLifetime Lifetime
        {
            get { return ServiceLifetime.Scoped; }
        }

        public Type ServiceType => typeof(IServiceScopeFactory);

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            return this;
        }
    }
}
