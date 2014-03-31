using System;
using Microsoft.AspNet.DependencyInjection.Ninject;
using Microsoft.AspNet.DependencyInjection.Tests.Fakes;
using Ninject;

namespace Microsoft.AspNet.DependencyInjection.Tests
{
    public class NinjectContainerTests : ScopingContainerTestBase
    {
        protected override IServiceProvider CreateContainer()
        {
            return CreateContainer(new FakeFallbackServiceProvider());
        }

        protected override IServiceProvider CreateContainer(IServiceProvider fallbackProvider)
        {
            IKernel kernel = new StandardKernel();

            NinjectRegistration.Populate(kernel, TestServices.DefaultServices(), fallbackProvider);

            return kernel.Get<IServiceProvider>();
        }
    }
}