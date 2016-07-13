// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ScopedCallSite : IServiceCallSite
    {
        internal IService Key { get; }
        internal IServiceCallSite ServiceCallSite { get; }

        public ScopedCallSite(IService key, IServiceCallSite serviceCallSite)
        {
            Key = key;
            ServiceCallSite = serviceCallSite;
        }
    }
}