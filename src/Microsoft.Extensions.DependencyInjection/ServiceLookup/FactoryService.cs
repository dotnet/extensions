// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class FactoryService : IService, IServiceCallSite
    {
        internal ServiceDescriptor Descriptor { get; }

        public FactoryService(ServiceDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public IService Next { get; set; }

        public ServiceLifetime Lifetime
        {
            get { return Descriptor.Lifetime; }
        }

        public Type ServiceType => Descriptor.ServiceType;

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            return this;
        }
    }
}
