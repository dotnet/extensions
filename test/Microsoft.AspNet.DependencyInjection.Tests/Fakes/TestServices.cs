using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public static class TestServices
    {
        public static IEnumerable<IServiceDescriptor> DefaultServices()
        {
            return new IServiceDescriptor[]
            {
                new ServiceDescriptor<IFakeService, FakeService>(),
                new ServiceDescriptor<IFakeMultipleService, FakeOneMultipleService>(),
                new ServiceDescriptor<IFakeMultipleService, FakeTwoMultipleService>(),
                new ServiceDescriptor<IFakeOuterService, FakeOuterService>(),
                new ServiceInstanceDescriptor<IFakeServiceInstance>(new FakeService() { Message = "Instance" }),
                new ServiceDescriptor<IFakeScopedService, FakeService>(LifecycleKind.Scoped),
                new ServiceDescriptor<IFakeSingletonService, FakeService>(LifecycleKind.Singleton)
            };
        }

        public class ServiceDescriptor<TService, TImplementation> : IServiceDescriptor
        {
            public ServiceDescriptor(LifecycleKind lifecycle = LifecycleKind.Transient)
            {
                Lifecycle = lifecycle;
            }

            public LifecycleKind Lifecycle { get; private set; }

            public Type ServiceType
            {
                get { return typeof (TService); }
            }

            public Type ImplementationType
            {
                get { return typeof (TImplementation); }
            }

            public object ImplementationInstance
            {
                get { return null; }
            }
        }

        public class ServiceInstanceDescriptor<TService> : IServiceDescriptor
        {
            public ServiceInstanceDescriptor(object instance)
            {
                ImplementationInstance = instance;
            }

            public LifecycleKind Lifecycle
            {
                get { return LifecycleKind.Singleton; }
            }

            public Type ServiceType
            {
                get { return typeof (TService); }
            }

            public Type ImplementationType
            {
                get { return null; }
            }

            public object ImplementationInstance { get; private set; }
        }
    }
}