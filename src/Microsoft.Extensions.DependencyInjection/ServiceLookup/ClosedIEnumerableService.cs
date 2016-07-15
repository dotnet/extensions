// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ClosedIEnumerableService : IService
    {
        private readonly Type _itemType;
        private readonly ServiceEntry _serviceEntry;

        public ClosedIEnumerableService(Type itemType, ServiceEntry entry)
        {
            _itemType = itemType;
            _serviceEntry = entry;
        }

        public IService Next { get; set; }

        public ServiceLifetime Lifetime
        {
            get { return ServiceLifetime.Transient; }
        }

        public Type ServiceType => _itemType;

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            var list = new List<IServiceCallSite>();
            var service = _serviceEntry.First;
            while (service != null)
            {
                list.Add(provider.GetResolveCallSite(service, callSiteChain));
                service = service.Next;
            }
            return new ClosedIEnumerableCallSite(_itemType, list.ToArray());
        }
    }
}
