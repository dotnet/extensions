using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public ServiceCollection AddSingleton<TService>()
        {
            AddSingleton<TService, TService>();
            return this;
        }
        
        public ServiceCollection AddTransient<TService>()
        {
            AddTransient<TService, TService>();
            return this;
        }

        public ServiceCollection AddScoped<TService>()
        {
            AddScoped<TService, TService>();
            return this;
        }

        // TODO: Move AddSetup to yet to be created IServiceCollection
        // https://github.com/aspnet/DependencyInjection/issues/73
        public ServiceCollection AddSetup(Type setupType)
        {
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IOptionsSetup<>));
            foreach (var serviceType in serviceTypes)
            {
                Add(new ServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = setupType,
                    Lifecycle = LifecycleKind.Transient
                });
            }
            return this;
        }

        public ServiceCollection AddSetup<TSetup>()
        {
            return AddSetup(typeof(TSetup));
        }

        public ServiceCollection AddSetup(object setupInstance)
        {
            if (setupInstance == null)
            {
                throw new ArgumentNullException("setupInstance");
            }
            var setupType = setupInstance.GetType();
            var serviceTypes = setupType.GetTypeInfo().ImplementedInterfaces
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(IOptionsSetup<>));
            foreach (var serviceType in serviceTypes)
            {
                Add(new ServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationInstance = setupInstance,
                    Lifecycle = LifecycleKind.Singleton
                });
            }
            return this;
        }

        public ServiceCollection AddInstance<TService>(TService implementationInstance)
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
