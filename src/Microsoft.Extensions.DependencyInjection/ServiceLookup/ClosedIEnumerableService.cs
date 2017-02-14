// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ClosedIEnumerableService : IService
    {
        private readonly ServiceEntry _serviceEntry;

        public ClosedIEnumerableService(Type itemType, ServiceEntry entry)
        {
            ServiceType = itemType;
            _serviceEntry = entry;
            ImplementationType = itemType.MakeArrayType();
        }

        public IService Next { get; set; }

        public ServiceLifetime Lifetime
        {
            get { return ServiceLifetime.Transient; }
        }

        public Type ServiceType { get; }

        public Type ImplementationType { get; }

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            var list = new List<IServiceCallSite>();
            var service = _serviceEntry.First;
            while (service != null)
            {
                list.Add(provider.GetResolveCallSite(service, callSiteChain));
                service = service.Next;
            }
            return new ClosedIEnumerableCallSite(ServiceType, list.ToArray());
        }
    }
}
