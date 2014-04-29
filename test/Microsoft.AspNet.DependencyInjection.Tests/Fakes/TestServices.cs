using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public static class TestServices
    {
        public static IEnumerable<IServiceDescriptor> DefaultServices()
        {
            var describer = new ServiceDescriber();

            yield return describer.Transient<IFakeService, FakeService>();
            yield return describer.Transient<IFakeMultipleService, FakeOneMultipleService>();
            yield return describer.Transient<IFakeMultipleService, FakeTwoMultipleService>();
            yield return describer.Transient<IFakeOuterService, FakeOuterService>();
            yield return describer.Instance<IFakeServiceInstance>(new FakeService() { Message = "Instance" });
            yield return describer.Scoped<IFakeScopedService, FakeService>();
            yield return describer.Singleton<IFakeSingletonService, FakeService>();
            yield return describer.Transient<IFakeFallbackService, FakeService>();
            yield return describer.Describe(
                typeof(IFakeOpenGenericService<string>),
                typeof(FakeService),
                implementationInstance: null,
                lifecycle: LifecycleKind.Transient);
            yield return describer.Describe(
                typeof(IFakeOpenGenericService<>),
                typeof(FakeOpenGenericService<>),
                implementationInstance: null,
                lifecycle: LifecycleKind.Transient);

            yield return describer.Singleton<IOptionsAccessor<FakeOptions>, OptionsAccessor<FakeOptions>>();

            ServiceCollection services = new ServiceCollection();
            services.AddSetup<FakeOptionsSetupC>();
            services.AddSetup(new FakeOptionsSetupB());
            services.AddSetup(typeof(FakeOptionsSetupA));
            foreach (var description in services)
            {
                yield return description;
            }
        }
    }
}