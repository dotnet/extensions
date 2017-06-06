// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ClosedIEnumerableCallSite : IServiceCallSite
    {
        internal Type ItemType { get; }
        internal IServiceCallSite[] ServiceCallSites { get; }

        public ClosedIEnumerableCallSite(Type itemType, IServiceCallSite[] serviceCallSites)
        {
            ItemType = itemType;
            ServiceCallSites = serviceCallSites;
        }
    }
}