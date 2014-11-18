// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceCollectionExtensionTest
    {
        private static readonly Func<IServiceProvider, IFakeService> _factory = _ => new FakeService();
        private static readonly FakeService _instance = new FakeService();

        public static TheoryData AddImplementationTypeData
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeService);
                return new TheoryData<Action<IServiceCollection>, Type, Type, LifecycleKind>
                {
                    { collection => collection.AddTransient(serviceType, implementationType), serviceType, implementationType, LifecycleKind.Transient },
                    { collection => collection.AddTransient<IFakeService, FakeService>(), serviceType, implementationType, LifecycleKind.Transient },
                    { collection => collection.AddTransient<IFakeService>(), serviceType, serviceType, LifecycleKind.Transient },
                    { collection => collection.AddTransient(implementationType), implementationType, implementationType, LifecycleKind.Transient },

                    { collection => collection.AddScoped(serviceType, implementationType), serviceType, implementationType, LifecycleKind.Scoped },
                    { collection => collection.AddScoped<IFakeService, FakeService>(), serviceType, implementationType, LifecycleKind.Scoped },
                    { collection => collection.AddScoped<IFakeService>(), serviceType, serviceType, LifecycleKind.Scoped },
                    { collection => collection.AddScoped(implementationType), implementationType, implementationType, LifecycleKind.Scoped },

                    { collection => collection.AddSingleton(serviceType, implementationType), serviceType, implementationType, LifecycleKind.Singleton },
                    { collection => collection.AddSingleton<IFakeService, FakeService>(), serviceType, implementationType, LifecycleKind.Singleton },
                    { collection => collection.AddSingleton<IFakeService>(), serviceType, serviceType, LifecycleKind.Singleton },
                    { collection => collection.AddSingleton(implementationType), implementationType, implementationType, LifecycleKind.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddImplementationTypeData))]
        public void AddWithTypeAddsServiceWithRightLifecyle(Action<IServiceCollection> addTypeAction,
                                                            Type expectedServiceType,
                                                            Type expectedImplementationType,
                                                            LifecycleKind lifeCycle)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addTypeAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(expectedServiceType, descriptor.ServiceType);
            Assert.Equal(expectedImplementationType, descriptor.ImplementationType);
            Assert.Equal(lifeCycle, descriptor.Lifecycle);
        }

        public static TheoryData AddImplementationFactoryData
        {
            get
            {
                var serviceType = typeof(IFakeService);

                return new TheoryData<Action<IServiceCollection>, LifecycleKind>
                {
                    { collection => collection.AddTransient<IFakeService>(_factory), LifecycleKind.Transient },
                    { collection => collection.AddTransient(serviceType, _factory), LifecycleKind.Transient },

                    { collection => collection.AddScoped<IFakeService>(_factory), LifecycleKind.Scoped },
                    { collection => collection.AddScoped(serviceType, _factory), LifecycleKind.Scoped },

                    { collection => collection.AddSingleton<IFakeService>(_factory), LifecycleKind.Singleton },
                    { collection => collection.AddSingleton(serviceType, _factory), LifecycleKind.Singleton },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddImplementationFactoryData))]
        public void AddWithFactoryAddsServiceWithRightLifecyle(Action<IServiceCollection> addAction,
                                                               LifecycleKind lifeCycle)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Same(_factory, descriptor.ImplementationFactory);
            Assert.Equal(lifeCycle, descriptor.Lifecycle);
        }

        public static TheoryData AddInstanceData
        {
            get
            {
                return new TheoryData<Action<IServiceCollection>>
                {
                    { collection => collection.AddInstance<IFakeService>(_instance) },
                    { collection => collection.AddInstance(typeof(IFakeService), _instance) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddInstanceData))]
        public void AddInstance_AddsWithSingletonLifecycle(Action<IServiceCollection> addAction)
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            addAction(collection);

            // Assert
            var descriptor = Assert.Single(collection);
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Same(_instance, descriptor.ImplementationInstance);
            Assert.Equal(LifecycleKind.Singleton, descriptor.Lifecycle);
        }
    }
}