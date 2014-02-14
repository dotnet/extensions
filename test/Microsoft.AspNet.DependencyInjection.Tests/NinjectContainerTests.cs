using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Ninject;
using Ninject.Modules;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class NinjectContainerTests : AllContainerTestsBase
    {
        protected override IServiceProvider CreateContainer()
        {
            IKernel kernel = new StandardKernel(new BindServiceDescriptorsModule(TestServices.DefaultServices()));

            kernel.Bind<IServiceProvider>().To<NinjectServiceProvider>();

            return kernel.Get<IServiceProvider>();
        }

        private class BindServiceDescriptorsModule : NinjectModule
        {
            private IEnumerable<IServiceDescriptor> _serviceDescriptors;

            public BindServiceDescriptorsModule(IEnumerable<IServiceDescriptor> serviceDescriptors)
            {
                _serviceDescriptors = serviceDescriptors;
            }

            public override void Load()
            {
                foreach (var descriptor in _serviceDescriptors)
                {
                    Bind(descriptor.ServiceType).To(descriptor.ImplementationType);
                }
            }
        }

        private class NinjectServiceProvider : IServiceProvider
        {
            private IKernel _kernel;

            public NinjectServiceProvider(IKernel kernel)
            {
                _kernel = kernel;
            }

            public object GetService(Type type)
            {
                return _kernel.TryGet(type) ?? GetMultiService(type);
            }

            private object GetMultiService(Type collectionType)
            {
                if (IsIEnumerable(collectionType))
                {
                    Type serviceType = collectionType.GetTypeInfo().GenericTypeArguments.Single();

                    // _kernel.GetAll(Type) returns IEnumerable<object>.
                    // We need to return IEnumerable<{serviceType}> so we copy everything
                    // out into our own List<{serviceType}> and return that.
                    IList services = CreateEmptyServiceList(serviceType);

                    foreach (object service in _kernel.GetAll(serviceType))
                    {
                        services.Add(service);
                    }

                    return services;
                }

                return null;
            }

            private static bool IsIEnumerable(Type type)
            {
                return type.GetTypeInfo().IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            }

            private static IList CreateEmptyServiceList(Type serviceType)
            {
                Type listType = typeof(List<>).MakeGenericType(serviceType);
                return (IList)Activator.CreateInstance(listType);
            }
        }
    }
}