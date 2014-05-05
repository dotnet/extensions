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

using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class TypeActivatorTests
    {
        [Fact]
        public void TypeActivatorEnablesYouToCreateAnyTypeWithServicesEvenWhenNotInIocContainer()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<AnotherClass>(serviceProvider);

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod]", result);
        }

        [Fact]
        public void TypeActivatorAcceptsAnyNumberOfAdditionalConstructorParametersToProvide()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<AnotherClassAcceptingData>(serviceProvider, "1", "2");

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod] 1 2", result);
        }

        [Fact]
        public void TypeActivatorWorksWithStaticCtor()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<ClassWithStaticCtor>(serviceProvider);

            Assert.NotNull(anotherClass);
        }
    }
}
