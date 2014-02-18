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
                    if (descriptor.ImplementationType != null)
                    {
                        Bind(descriptor.ServiceType).To(descriptor.ImplementationType);
                    }
                    else
                    {
                        Bind(descriptor.ServiceType).ToConstant(descriptor.ImplementationInstance);
                    }
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
                return MultiServiceHelpers.GetMultiService(collectionType,
                    serviceType => _kernel.GetAll(serviceType));
            }
        }
    }
}