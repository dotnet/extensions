using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class FuncCallSite : ServiceCallSite
    {
        internal Type ItemType { get; }
        internal ServiceCallSite ItemCallSite { get; }

        public FuncCallSite(ResultCache cache, Type itemType, ServiceCallSite itemCallSite) : base(cache)
        {
            ItemType = itemType;
            ItemCallSite = itemCallSite;
        }

        public override Type ServiceType => typeof(IEnumerable<>).MakeGenericType(ItemType);
        public override Type ImplementationType => typeof(Func<>).MakeGenericType(ItemType);
        public override CallSiteKind Kind { get; } = CallSiteKind.Func;
    }
}
