// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class ServiceTest
    {
        [Theory]
        [InlineData(typeof(TypeWithNoConstructors))]
        [InlineData(typeof(TypeWithNoPublicConstructors))]
        [InlineData(typeof(TypeWithParameterlessConstructor))]
        [InlineData(typeof(TypeWithParameterlessPublicConstructor))]
        public void CreateCallSite_CreatesInstanceCallSite_IfTypeHasNoPublicConstructors(Type type)
        {
            // Arrange
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);
            var serviceProvider = new ServiceProvider(new[] { descriptor });

            // Act
            var callSite = service.CreateCallSite(serviceProvider, new HashSet<Type>());

            // Assert
            Assert.IsType<CreateInstanceCallSite>(callSite);
        }

        [Theory]
        [InlineData(typeof(TypeWithParameterizedConstructor))]
        [InlineData(typeof(TypeWithParameterizedAndNullaryConstructor))]
        [InlineData(typeof(TypeWithMultipleParameterizedConstructors))]
        [InlineData(typeof(TypeWithSupersetConstructors))]
        public void CreateCallSite_CreatesConstructorCallSite_IfTypeHasConstructorWithInjectableParameters(Type type)
        {
            // Arrange
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);
            var serviceProvider = GetServiceProvider(
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), new FakeService())
            );

            // Act
            var callSite = service.CreateCallSite(serviceProvider, new HashSet<Type>());

            // Assert
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(new[] { typeof(IFakeService) }, GetParameters(constructorCallSite));
        }

        [Fact]
        public void CreateCallSite_CreatesConstructorWithEnumerableParameters()
        {
            // Arrange
            var type = typeof(TypeWithEnumerableConstructors);
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);
            var serviceProvider = GetServiceProvider(
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), new FakeService())
            );

            // Act
            var callSite = service.CreateCallSite(serviceProvider, new HashSet<Type>());

            // Assert
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(
                new[] { typeof(IEnumerable<IFakeService>), typeof(IEnumerable<IFactoryService>) },
                GetParameters(constructorCallSite));
        }

        [Fact]
        public void CreateCallSite_UsesNullaryConstructorIfServicesCannotBeInjectedIntoOtherConstructors()
        {
            // Arrange
            var type = typeof(TypeWithParameterizedAndNullaryConstructor);
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);
            var serviceProvider = new ServiceProvider(new[] { descriptor });

            // Act
            var callSite = service.CreateCallSite(serviceProvider, new HashSet<Type>());

            // Assert
            Assert.IsType<CreateInstanceCallSite>(callSite);
        }

        public static TheoryData CreateCallSite_PicksConstructorWithTheMostNumberOfResolvedParametersData =>
            new TheoryData<Type, ServiceProvider, Type[]>
            {
                {
                    typeof(TypeWithSupersetConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService))
                    ),
                    new[] { typeof(IFakeService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FactoryService))
                    ),
                    new[] { typeof(IFactoryService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FactoryService))
                    ),
                    new[] { typeof(IFakeService), typeof(IFactoryService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FactoryService))
                    ),
                    new[] { typeof(IFakeService), typeof(IFakeMultipleService), typeof(IFactoryService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FactoryService)),
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService))
                    ),
                    new[] { typeof(IFakeMultipleService), typeof(IFactoryService), typeof(IFakeService), typeof(IFakeScopedService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FactoryService)),
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService))
                    ),
                    new[] { typeof(IFakeMultipleService), typeof(IFactoryService), typeof(IFakeService), typeof(IFakeScopedService) }
                },
                {
                    typeof(TypeWithGenericServices),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFakeService), typeof(IFakeOpenGenericService<IFakeService>) }
                },
                {
                    typeof(TypeWithGenericServices),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFakeService), typeof(IFactoryService), typeof(IFakeOpenGenericService<IFakeService>) }
                }
            };

        [Theory]
        [MemberData(nameof(CreateCallSite_PicksConstructorWithTheMostNumberOfResolvedParametersData))]
        public void CreateCallSite_PicksConstructorWithTheMostNumberOfResolvedParameters(
            Type type,
            IServiceProvider serviceProvider,
            Type[] expectedConstructorParameters)
        {
            // Arrange
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);


            // Act
            var callSite = service.CreateCallSite((ServiceProvider)serviceProvider, new HashSet<Type>());

            // Assert
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(expectedConstructorParameters, GetParameters(constructorCallSite));
        }

        public static TheoryData CreateCallSite_ConsidersConstructorsWithDefaultValuesData =>
            new TheoryData<ServiceProvider, Type[]>
            {
                {
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFakeMultipleService), typeof(IFakeService) }
                },
                {
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFactoryService), typeof(IFakeScopedService) }
                },
                {
                   GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFactoryService), typeof(IFakeScopedService) }
                }
            };

        [Theory]
        [MemberData(nameof(CreateCallSite_ConsidersConstructorsWithDefaultValuesData))]
        public void CreateCallSite_ConsidersConstructorsWithDefaultValues(
            IServiceProvider serviceProvider,
            Type[] expectedConstructorParameters)
        {
            // Arrange
            var type = typeof(TypeWithDefaultConstructorParameters);
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);

            // Act
            var callSite = service.CreateCallSite((ServiceProvider)serviceProvider, new HashSet<Type>());

            // Assert
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(expectedConstructorParameters, GetParameters(constructorCallSite));
        }

        [Fact]
        public void CreateCallSite_ThrowsIfTypeHasSingleConstructorWithResolvableParameters()
        {
            // Arrange
            var type = typeof(TypeWithParameterizedConstructor);
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);
            var serviceProvider = GetServiceProvider();

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => service.CreateCallSite(serviceProvider, new HashSet<Type>()));
            Assert.Equal($"Unable to resolve service for type '{typeof(IFakeService)}' while attempting to activate '{type}'.",
                ex.Message);
        }

        [Theory]
        [InlineData(typeof(TypeWithMultipleParameterizedConstructors))]
        [InlineData(typeof(TypeWithSupersetConstructors))]
        [InlineData(typeof(TypeWithSupersetConstructors))]
        public void CreateCallSite_ThrowsIfTypeHasNoConstructurWithResolvableParameters(Type type)
        {
            // Arrange
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);
            var serviceProvider = GetServiceProvider(
                new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService), ServiceLifetime.Transient)
            );

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => service.CreateCallSite(serviceProvider, new HashSet<Type>()));
            Assert.Equal($"No constructor for type '{type}' can be instantiated using services from the service container and default values.",
                ex.Message);
        }

        public static TheoryData CreateCallSite_ThrowsIfMultipleNonOverlappingConstructorsCanBeResolvedData =>
            new TheoryData<Type, ServiceProvider, Type[][]>
            {
                {
                    typeof(TypeWithDefaultConstructorParameters),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[]
                    {
                        new[] { typeof(IFakeMultipleService), typeof(IFakeService) },
                        new[] { typeof(IFactoryService), typeof(IFakeScopedService) }
                    }
                },
                {
                    typeof(TypeWithMultipleParameterizedConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[]
                    {
                        new[] { typeof(IFactoryService) },
                        new[] { typeof(IFakeService) }
                    }
                },
                {
                    typeof(TypeWithNonOverlappedConstructors),
                    GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeOuterService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[]
                    {
                        new[] { typeof(IFakeScopedService), typeof(IFakeService), typeof(IFakeMultipleService) },
                        new[] { typeof(IFakeOuterService) }
                    }
                },
                {
                   typeof(TypeWithUnresolvableEnumerableConstructors),
                   GetServiceProvider(
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                   new[]
                   {
                        new[] { typeof(IFakeService) },
                        new[] { typeof(IEnumerable<IFakeService>) }
                   }
                },
                {
                   typeof(TypeWithUnresolvableEnumerableConstructors),
                   GetServiceProvider(
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                   new[]
                   {
                        new[] { typeof(IEnumerable<IFakeService>) },
                        new[] { typeof(IFactoryService) }
                   }
                },
            };

        [Theory]
        [MemberData(nameof(CreateCallSite_ThrowsIfMultipleNonOverlappingConstructorsCanBeResolvedData))]
        public void CreateCallSite_ThrowsIfMultipleNonOverlappingConstructorsCanBeResolved(
            Type type,
            IServiceProvider serviceProvider,
            Type[][] expectedConstructorParameterTypes)
        {
            // Arrange
            var expectedMessage =
                string.Join(
                    Environment.NewLine,
                    $"Unable to activate type '{type}'. The following constructors are ambigious:",
                    GetConstructor(type, expectedConstructorParameterTypes[0]),
                    GetConstructor(type, expectedConstructorParameterTypes[1]));
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var service = new Service(descriptor);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => service.CreateCallSite((ServiceProvider)serviceProvider, new HashSet<Type>()));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CreateCallSite_ThrowsIfMultipleNonOverlappingConstructorsForGenericTypesCanBeResolved()
        {
            // Arrange
            var type = typeof(TypeWithGenericServices);
            var expectedMessage = $"Unable to activate type '{type}'. The following constructors are ambigious:";
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var serviceProvider = GetServiceProvider(
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), ServiceLifetime.Transient)
            );
            var service = new Service(descriptor);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => service.CreateCallSite(serviceProvider, new HashSet<Type>()));
            Assert.StartsWith(expectedMessage, ex.Message);
        }

        private static ServiceProvider GetServiceProvider(params ServiceDescriptor[] descriptors)
        {
            var collection = new ServiceCollection();
            foreach (var descriptor in descriptors)
            {
                collection.Add(descriptor);
            }

            return (ServiceProvider)collection.BuildServiceProvider();
        }

        private static IEnumerable<Type> GetParameters(ConstructorCallSite constructorCallSite) =>
            constructorCallSite
                .Constructor
                .GetParameters()
                .Select(p => p.ParameterType);

        private static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes) =>
            type.GetTypeInfo().DeclaredConstructors.First(
                c => Enumerable.SequenceEqual(
                    c.GetParameters().Select(p => p.ParameterType),
                    parameterTypes));
    }
}
