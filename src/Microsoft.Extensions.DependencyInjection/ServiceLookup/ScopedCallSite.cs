// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ScopedCallSite : IServiceCallSite
    {
        internal IServiceCallSite ServiceCallSite { get; }

        public ScopedCallSite(IServiceCallSite serviceCallSite)
        {
            ServiceCallSite = serviceCallSite;
        }

        public Type ServiceType => ServiceCallSite.ServiceType;
        public Type ImplementationType => ServiceCallSite.ImplementationType;

        protected bool Equals(ScopedCallSite other)
        {
            return ServiceType == other.ServiceType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScopedCallSite) obj);
        }

        public override int GetHashCode()
        {
            return (ServiceType != null ? ServiceType.GetHashCode() : 0);
        }
    }
}