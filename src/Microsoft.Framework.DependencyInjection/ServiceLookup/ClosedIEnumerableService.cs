// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
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

        public LifecycleKind Lifecycle
        {
            get { return LifecycleKind.Transient; }
        }

        public object Create(ServiceProvider provider)
        {
            var list = new List<object>();
            for (var service = _serviceEntry.First; service != null; service = service.Next)
            {
                list.Add(provider.ResolveService(service));
            }
            var array = Array.CreateInstance(_itemType, list.Count);
            Array.Copy(list.ToArray(), array, list.Count);
            return array;
        }
    }
}
