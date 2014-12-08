// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class TypeActivatorTests
    {
        public delegate object CreateInstanceFunc(ITypeActivator activator, IServiceProvider provider, Type type, object[] args);

        public static object CreateInstanceDirectly(ITypeActivator activator, IServiceProvider provider, Type type, object[] args)
        {
            return activator.CreateInstance(provider, type, args);
        }

        public static object CreateInstanceFromFactory(ITypeActivator activator, IServiceProvider provider, Type type, object[] args)
        {
            var factory = activator.CreateFactory(type, args.Select(a => a.GetType()).ToArray());
            return factory(provider, args);
        }

        public static T CreateInstance<T>(CreateInstanceFunc func, ITypeActivator activator, IServiceProvider provider, params object[] args)
        {
            return (T)func(activator, provider, typeof(T), args);
        }

        public static IEnumerable<object[]> CreateInstanceFuncs
        {
            get
            {
                yield return new[] { (CreateInstanceFunc)CreateInstanceDirectly };
                yield return new[] { (CreateInstanceFunc)CreateInstanceFromFactory };
            }
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorEnablesYouToCreateAnyTypeWithServicesEvenWhenNotInIocContainer(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = CreateInstance<AnotherClass>(createFunc, typeActivator, serviceProvider);

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod]", result);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorAcceptsAnyNumberOfAdditionalConstructorParametersToProvide(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = CreateInstance<AnotherClassAcceptingData>(createFunc, typeActivator, serviceProvider, "1", "2");

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod] 1 2", result);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithStaticCtor(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = CreateInstance<ClassWithStaticCtor>(createFunc, typeActivator, serviceProvider);

            Assert.NotNull(anotherClass);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithCtorWithOptionalArgs(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var anotherClass = CreateInstance<ClassWithOptionalArgsCtor>(createFunc, typeActivator, serviceProvider);

            Assert.NotNull(anotherClass);
            Assert.Equal("BLARGH", anotherClass.Whatever);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorCanDisambiguateConstructorsWithUniqueArguments(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var instance = CreateInstance<ClassWithAmbiguousCtors>(createFunc, typeActivator, serviceProvider, "1", 2);

            Assert.NotNull(instance);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRequiresPublicConstructor(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<ClassWithPrivateCtor>(createFunc, typeActivator, serviceProvider));

            Assert.Equal(Resources.FormatNoConstructorMatch(typeof(ClassWithPrivateCtor)), ex.Message);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRequiresAllArgumentsCanBeAccepted(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var ex1 = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<AnotherClassAcceptingData>(createFunc, typeActivator, serviceProvider, "1", "2", "3"));
            var ex2 = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<AnotherClassAcceptingData>(createFunc, typeActivator, serviceProvider, 1, 2));

            Assert.Equal(Resources.FormatNoConstructorMatch(typeof(AnotherClassAcceptingData)), ex1.Message);
            Assert.Equal(Resources.FormatNoConstructorMatch(typeof(AnotherClassAcceptingData)), ex2.Message);
        }

        [Fact]
        public void TypeActivatorCreateFactoryDoesNotAllowForAmbiguousConstructorMatches()
        {
            var typeActivator = new TypeActivator();

            var ex1 = Assert.Throws<InvalidOperationException>(() =>
                typeActivator.CreateFactory(typeof(ClassWithAmbiguousCtors), new[] { typeof(string) }));
            var ex2 = Assert.Throws<InvalidOperationException>(() =>
                typeActivator.CreateFactory(typeof(ClassWithAmbiguousCtors), new[] { typeof(int) }));

            Assert.Equal(Resources.FormatAmbiguousConstructorMatch(typeof(ClassWithAmbiguousCtors)), ex1.Message);
            Assert.Equal(Resources.FormatAmbiguousConstructorMatch(typeof(ClassWithAmbiguousCtors)), ex2.Message);
        }

        [Fact]
        public void GetServiceOrCreateInstanceRegisteredServiceTransient()
        {
            // Reset the count because test order is not guaranteed
            CreationCountFakeService.InstanceCount = 0;

            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<CreationCountFakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var service = typeActivator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);

            service = typeActivator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(2, service.InstanceId);
            Assert.Equal(2, CreationCountFakeService.InstanceCount);
        }

        [Fact]
        public void GetServiceOrCreateInstanceRegisteredServiceSingleton()
        {
            // Reset the count because test order is not guaranteed
            CreationCountFakeService.InstanceCount = 0;

            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddSingleton<CreationCountFakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var service = typeActivator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);

            service = typeActivator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);
        }

        [Fact]
        public void GetServiceOrCreateInstanceUnregisteredService()
        {
            // Reset the count because test order is not guaranteed
            CreationCountFakeService.InstanceCount = 0;

            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .AddTransient<ITypeActivator, TypeActivator>()
                .BuildServiceProvider();

            var typeActivator = serviceProvider.GetService<ITypeActivator>();

            var service = (CreationCountFakeService)typeActivator.GetServiceOrCreateInstance(serviceProvider, typeof(CreationCountFakeService));
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);

            service = typeActivator.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(2, service.InstanceId);
            Assert.Equal(2, CreationCountFakeService.InstanceCount);
        }
    }
}