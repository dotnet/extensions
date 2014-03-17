using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.ConfigurationModel;

namespace Microsoft.AspNet.DependencyInjection
{
    public class ServiceCollection : IEnumerable<IServiceDescriptor>
    {
        private readonly List<IServiceDescriptor> _descriptors;
        private readonly ServiceDescriber _describe;

        public ServiceCollection()
            : this(fallbackServices: null)
        {
        }

        public ServiceCollection(IServiceProvider fallbackServices)
            : this(fallbackServices, new Configuration())
        {
        }

        public ServiceCollection(IServiceProvider fallbackServices, IConfiguration configuration)
        {
            _descriptors = new List<IServiceDescriptor>();
            _describe = new ServiceDescriber(configuration);

            FallbackServices = fallbackServices;
        }

        public IServiceProvider FallbackServices { get; set; }

        public void Add(IServiceDescriptor descriptor)
        {
            _descriptors.Add(descriptor);
        }

        public void Add(
                IEnumerable<IServiceDescriptor> descriptors,
                params IEnumerable<IServiceDescriptor>[] moreDescriptors)
        {
            _descriptors.AddRange(descriptors);
            _descriptors.AddRange(moreDescriptors.SelectMany(d => d));
        }

        public ServiceCollection AddTransient<TService, TImplementation>()
        {
            Add(_describe.Transient<TService, TImplementation>());
            return this;
        }

        public ServiceCollection AddScoped<TService, TImplementation>()
        {
            Add(_describe.Scoped<TService, TImplementation>());
            return this;
        }

        public ServiceCollection AddSingleton<TService, TImplementation>()
        {
            Add(_describe.Singleton<TService, TImplementation>());
            return this;
        }

        public ServiceCollection AddInstance<TService>(object implementationInstance)
        {
            Add(_describe.Instance<TService>(implementationInstance));
            return this;
        }

        public IEnumerator<IServiceDescriptor> GetEnumerator()
        {
            return _descriptors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
