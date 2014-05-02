// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
            services.SetupOptions<FakeOptions>(o => o.Message += "a", -100);
            services.AddSetup<FakeOptionsSetupC>();
            services.AddSetup(new FakeOptionsSetupB());
            services.AddSetup(typeof(FakeOptionsSetupA));
            services.SetupOptions<FakeOptions>(o => o.Message += "z", 10000);
            foreach (var description in services)
            {
                yield return description;
            }
        }
    }
}