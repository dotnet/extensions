using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.DependencyInjection.ServiceLookup
{
    internal class ServiceTable
    {
        private readonly object _sync = new object();

        private readonly Dictionary<Type, ServiceEntry> _services;
        private readonly Dictionary<Type, List<IGenericService>> _genericServices;

        public ServiceTable(IEnumerable<IServiceDescriptor> descriptors)
        {
            _services = new Dictionary<Type, ServiceEntry>();
            _genericServices = new Dictionary<Type, List<IGenericService>>();

            foreach (var descriptor in descriptors)
            {
                var serviceTypeInfo = descriptor.ServiceType.GetTypeInfo();
                if (!serviceTypeInfo.IsGenericTypeDefinition)
                {
                    Add(descriptor.ServiceType, new Service(descriptor));
                }
                else
                {
                    Add(descriptor.ServiceType, new GenericService(descriptor));
                }
            }
        }

        public bool TryGetEntry(Type serviceType, out ServiceEntry entry)
        {
            lock (_sync)
            {
                if (_services.TryGetValue(serviceType, out entry))
                {
                    return true;
                }
                else if (serviceType.GetTypeInfo().IsGenericType)
                {
                    var openServiceType = serviceType.GetGenericTypeDefinition();

                    List<IGenericService> genericEntry;
                    if (_genericServices.TryGetValue(openServiceType, out genericEntry))
                    {
                        foreach (var genericService in genericEntry)
                        {
                            var closedService = genericService.GetService(serviceType);
                            if (closedService != null)
                            {
                                Add(serviceType, closedService);
                            }
                        }

                        return _services.TryGetValue(serviceType, out entry);
                    }
                }
            }
            return false;
        }

        public void Add(Type serviceType, IService service)
        {
            lock (_sync)
            {
                ServiceEntry entry;
                if (_services.TryGetValue(serviceType, out entry))
                {
                    entry.Add(service);
                }
                else
                {
                    _services[serviceType] = new ServiceEntry(service);
                }
            }
        }

        public void Add(Type serviceType, IGenericService genericService)
        {
            lock (_sync)
            {
                List<IGenericService> genericEntry;
                if (!_genericServices.TryGetValue(serviceType, out genericEntry))
                {
                    genericEntry = new List<IGenericService>();
                    _genericServices[serviceType] = genericEntry;
                }

                genericEntry.Add(genericService);
            }
        }
    }
}
