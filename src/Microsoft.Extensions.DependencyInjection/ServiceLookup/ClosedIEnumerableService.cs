// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        public IServiceCallSite CreateCallSite(ServiceProvider provider, ISet<Type> callSiteChain)
        {
            var list = new List<IServiceCallSite>();
            var service = _serviceEntry.First;
            while (service != null)
            {
                list.Add(provider.GetResolveCallSite(service, callSiteChain));
                service = service.Next;
            }
            return new CallSite(_itemType, list.ToArray());
        }

        private class CallSite : IServiceCallSite
        {
            private readonly Type _itemType;
            private readonly IServiceCallSite[] _serviceCallSites;

            public CallSite(Type itemType, IServiceCallSite[] serviceCallSites)
            {
                _itemType = itemType;
                _serviceCallSites = serviceCallSites;
            }

            public object Invoke(ServiceProvider provider)
            {
                var array = Array.CreateInstance(_itemType, _serviceCallSites.Length);
                for (var index = 0; index < _serviceCallSites.Length; index++)
                {
                    array.SetValue(_serviceCallSites[index].Invoke(provider), index);
                }
                return array;
            }

            public Expression Build(Expression provider)
            {
                return Expression.NewArrayInit(
                    _itemType,
                    _serviceCallSites.Select(callSite =>
                        Expression.Convert(
                            callSite.Build(provider),
                            _itemType)));
            }
        }
    }
}
