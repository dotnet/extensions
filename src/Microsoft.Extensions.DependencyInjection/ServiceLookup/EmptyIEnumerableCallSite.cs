// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class EmptyIEnumerableCallSite : IServiceCallSite
    {
        internal object ServiceInstance { get; }
        internal Type ServiceType { get; }

        public EmptyIEnumerableCallSite(Type serviceType, object serviceInstance)
        {
            ServiceType = serviceType;
            ServiceInstance = serviceInstance;
        }
    }
}