using System;
using System.Reflection;

namespace Microsoft.AspNet.DependencyInjection.ServiceLookup
{
    internal class OpenIEnumerableService : IGenericService
    {
        private readonly ServiceTable _table;

        public OpenIEnumerableService(ServiceTable table)
        {
            _table = table;
        }

        public LifecycleKind Lifecycle
        {
            get { return LifecycleKind.Transient; }
        }

        public IService GetService(Type closedServiceType)
        {
            var itemType = closedServiceType.GetTypeInfo().GenericTypeArguments[0];

            ServiceEntry entry;
            return _table.TryGetEntry(itemType, out entry) ?
                new ClosedIEnumerableService(itemType, entry) :
                null;
        }
    }
}
