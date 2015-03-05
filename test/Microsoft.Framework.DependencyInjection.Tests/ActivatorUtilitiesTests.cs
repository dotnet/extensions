// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class ActivatorUtilitiesTests
    {
        public delegate object CreateInstanceFunc(IServiceProvider provider, Type type, object[] args);

        public static object CreateInstanceDirectly(IServiceProvider provider, Type type, object[] args)
        {
            return ActivatorUtilities.CreateInstance(provider, type, args);
        }

        public static object CreateInstanceFromFactory(IServiceProvider provider, Type type, object[] args)
        {
            var factory = ActivatorUtilities.CreateFactory(type, args.Select(a => a.GetType()).ToArray());
            return factory(provider, args);
        }

        public static T CreateInstance<T>(CreateInstanceFunc func, IServiceProvider provider, params object[] args)
        {
            return (T)func(provider, typeof(T), args);
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
                .BuildServiceProvider();

            var anotherClass = CreateInstance<AnotherClass>(createFunc, serviceProvider);

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod]", result);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorAcceptsAnyNumberOfAdditionalConstructorParametersToProvide(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .BuildServiceProvider();


            var anotherClass = CreateInstance<AnotherClassAcceptingData>(createFunc, serviceProvider, "1", "2");

            var result = anotherClass.LessSimpleMethod();

            Assert.Equal("[FakeServiceSimpleMethod] 1 2", result);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithStaticCtor(CreateInstanceFunc createFunc)
        {
            var anotherClass = CreateInstance<ClassWithStaticCtor>(createFunc, provider: null);

            Assert.NotNull(anotherClass);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithCtorWithOptionalArgs(CreateInstanceFunc createFunc)
        {
            var provider = new ServiceCollection().BuildServiceProvider();
            var anotherClass = CreateInstance<ClassWithOptionalArgsCtor>(createFunc, provider);

            Assert.NotNull(anotherClass);
            Assert.Equal("BLARGH", anotherClass.Whatever);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorCanDisambiguateConstructorsWithUniqueArguments(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .BuildServiceProvider();

            var instance = CreateInstance<ClassWithAmbiguousCtors>(createFunc, serviceProvider, "1", 2);

            Assert.NotNull(instance);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRequiresPublicConstructor(CreateInstanceFunc createFunc)
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<ClassWithPrivateCtor>(createFunc, provider: null));

            Assert.Equal(Resources.FormatNoConstructorMatch(typeof(ClassWithPrivateCtor)), ex.Message);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRequiresAllArgumentsCanBeAccepted(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddTransient<IFakeService, FakeService>()
                .BuildServiceProvider();

            var ex1 = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<AnotherClassAcceptingData>(createFunc, serviceProvider, "1", "2", "3"));
            var ex2 = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<AnotherClassAcceptingData>(createFunc, serviceProvider, 1, 2));

            Assert.Equal(Resources.FormatNoConstructorMatch(typeof(AnotherClassAcceptingData)), ex1.Message);
            Assert.Equal(Resources.FormatNoConstructorMatch(typeof(AnotherClassAcceptingData)), ex2.Message);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRethrowsOriginalExceptionFromConstructor(CreateInstanceFunc createFunc)
        {
            var ex1 = Assert.Throws<Exception>(() =>
                CreateInstance<ClassWithThrowingEmptyCtor>(createFunc, provider: null));

            var ex2 = Assert.Throws<Exception>(() =>
                CreateInstance<ClassWithThrowingCtor>(createFunc, provider: null, args: new[] { new FakeService() }));

            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }

        [Fact]
        public void TypeActivatorCreateFactoryDoesNotAllowForAmbiguousConstructorMatches()
        {
            var ex1 = Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateFactory(typeof(ClassWithAmbiguousCtors), new[] { typeof(string) }));
            var ex2 = Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateFactory(typeof(ClassWithAmbiguousCtors), new[] { typeof(int) }));

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
                .BuildServiceProvider();

            var service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);

            service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
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
                .BuildServiceProvider();

            var service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);

            service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
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
                .BuildServiceProvider();

            var service = (CreationCountFakeService)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, typeof(CreationCountFakeService));
            Assert.NotNull(service);
            Assert.Equal(1, service.InstanceId);
            Assert.Equal(1, CreationCountFakeService.InstanceCount);

            service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
            Assert.NotNull(service);
            Assert.Equal(2, service.InstanceId);
            Assert.Equal(2, CreationCountFakeService.InstanceCount);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void UnRegisteredServiceAsConstructorParameterThrowsException(CreateInstanceFunc createFunc)
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<CreationCountFakeService>()
                .BuildServiceProvider();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            CreateInstance<CreationCountFakeService>(createFunc, serviceProvider));
            Assert.Equal(Resources.FormatCannotResolveService(typeof(IFakeService), typeof(CreationCountFakeService)),
                ex.Message);
        }
    }
}