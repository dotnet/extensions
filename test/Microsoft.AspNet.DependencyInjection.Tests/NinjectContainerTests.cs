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
            IKernel kernel = new StandardKernel();

            NinjectRegistration.Populate(
                kernel,
                TestServices.DefaultServices(),
                new FakeFallbackServiceProvider());

            return kernel.Get<IServiceProvider>();
        }
    }
}