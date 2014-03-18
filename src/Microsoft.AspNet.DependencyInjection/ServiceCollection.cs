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
            : this(new Configuration())
        {
        }

        public ServiceCollection(IConfiguration configuration)
        {
            _descriptors = new List<IServiceDescriptor>();
            _describe = new ServiceDescriber(configuration);
        }

        public ServiceCollection Add(IServiceDescriptor descriptor)
        {
            _descriptors.Add(descriptor);
            return this;
        }

        public ServiceCollection Add(IEnumerable<IServiceDescriptor> descriptors)
        {
            _descriptors.AddRange(descriptors);
            return this;
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
