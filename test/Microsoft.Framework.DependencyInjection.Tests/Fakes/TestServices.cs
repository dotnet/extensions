// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.DependencyInjection.Tests.Fakes
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
            yield return describer.Transient<IDependOnNonexistentService, DependOnNonexistentService>();
            yield return describer.Describe(
                typeof(IFakeOpenGenericService<string>),
                typeof(FakeService),
                lifecycle: LifecycleKind.Transient);
            yield return describer.Describe(
                typeof(IFakeOpenGenericService<>),
                typeof(FakeOpenGenericService<>),
                lifecycle: LifecycleKind.Transient);

            yield return describer.Transient<IFactoryService>(provider =>
            {
                var fakeService = provider.GetService<IFakeService>();
                return new TransientFactoryService
                {
                    FakeService = fakeService,
                    Value = 42
                };
            });

            yield return describer.Scoped<ScopedFactoryService>(provider =>
            {
                var fakeService = provider.GetService<IFakeService>();
                return new ScopedFactoryService
                {
                    FakeService = fakeService,
                };
            });

            yield return describer.Transient<ServiceAcceptingFactoryService, ServiceAcceptingFactoryService>();
        }
    }
}