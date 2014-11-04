// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        [Fact]
        public void TypeActivatorWorksWithCtorWithOptionalArgs()
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = typeActivator.CreateInstance<ClassWithOptionalArgsCtor>(serviceProvider);

            Assert.NotNull(anotherClass);
            Assert.Equal("BLARGH", anotherClass.Whatever);
        }

    }
}
