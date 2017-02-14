// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class TransientCallSite : IServiceCallSite
    {
        internal IService Service { get; }
        internal IServiceCallSite ServiceCallSite { get; }

        public TransientCallSite(IService key, IServiceCallSite serviceCallSite)
        {
            Service = key;
            ServiceCallSite = serviceCallSite;
        }
    }
}