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
    public class CallSiteFactoryTest
    {
        [Fact]
        public void CreateCallSite_Throws_IfTypeHasNoPublicConstructors()
        {
            // Arrange
            var type = typeof(TypeWithNoPublicConstructors);
            var expectedMessage = $"A suitable constructor for type '{type}' could not be located. " +
                "Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => callSiteFactory(type));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData(typeof(TypeWithNoConstructors))]
        [InlineData(typeof(TypeWithParameterlessConstructor))]
        [InlineData(typeof(TypeWithParameterlessPublicConstructor))]
        public void CreateCallSite_CreatesInstanceCallSite_IfTypeHasDefaultOrPublicParameterlessConstructor(Type type)
        {
            // Arrange
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);

            // Act
            var callSite = callSiteFactory(type);

            // Assert
            Assert.Equal(CallSiteResultCacheLocation.Dispose, callSite.Cache.Location);
            var ctroCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Empty(ctroCallSite.ParameterCallSites);
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

            var callSiteFactory = GetCallSiteFactory(
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), new FakeService())
            );

            // Act
            var callSite = callSiteFactory(type);

            // Assert
            Assert.Equal(CallSiteResultCacheLocation.Dispose, callSite.Cache.Location);
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(new[] { typeof(IFakeService) }, GetParameters(constructorCallSite));
        }

        [Fact]
        public void CreateCallSite_CreatesConstructorWithEnumerableParameters()
        {
            // Arrange
            var type = typeof(TypeWithEnumerableConstructors);
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);

            var callSiteFactory = GetCallSiteFactory(
                descriptor,
                new ServiceDescriptor(typeof(IFakeService), new FakeService())
            );

            // Act
            var callSite = callSiteFactory(type);

            // Assert
            Assert.Equal(CallSiteResultCacheLocation.Dispose, callSite.Cache.Location);
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
            var callSiteFactory = GetCallSiteFactory(descriptor);

            // Act
            var callSite = callSiteFactory(type);

            // Assert
            Assert.Equal(CallSiteResultCacheLocation.Dispose, callSite.Cache.Location);
            var ctorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Empty(ctorCallSite.ParameterCallSites);
        }

        [Fact]
        public void CreateCallSite_ReturnsNull_IfClosedTypeDoesNotSatisfyStructGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithStructConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var nonMatchingType = typeof(IFakeOpenGenericService<object>);
            var nonMatchingCallSite = callSiteFactory(nonMatchingType);
            // Assert
            Assert.Null(nonMatchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsService_IfClosedTypeSatisfiesStructGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithStructConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var matchingType = typeof(IFakeOpenGenericService<int>);
            var matchingCallSite = callSiteFactory(matchingType);
            // Assert
            Assert.NotNull(matchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsNull_IfClosedTypeDoesNotSatisfyClassGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithClassConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var nonMatchingType = typeof(IFakeOpenGenericService<int>);
            var nonMatchingCallSite = callSiteFactory(nonMatchingType);
            // Assert
            Assert.Null(nonMatchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsService_IfClosedTypeSatisfiesClassGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithClassConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var matchingType = typeof(IFakeOpenGenericService<object>);
            var matchingCallSite = callSiteFactory(matchingType);
            // Assert
            Assert.NotNull(matchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsNull_IfClosedTypeDoesNotSatisfyNewGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithNewConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var nonMatchingType = typeof(IFakeOpenGenericService<TypeWithNoPublicConstructors>);
            var nonMatchingCallSite = callSiteFactory(nonMatchingType);
            // Assert
            Assert.Null(nonMatchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsService_IfClosedTypeSatisfiesNewGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithNewConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var matchingType = typeof(IFakeOpenGenericService<TypeWithParameterlessPublicConstructor>);
            var matchingCallSite = callSiteFactory(matchingType);
            // Assert
            Assert.NotNull(matchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsNull_IfClosedTypeDoesNotSatisfyInterfaceGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithInterfaceConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var nonMatchingType = typeof(IFakeOpenGenericService<int>);
            var nonMatchingCallSite = callSiteFactory(nonMatchingType);
            // Assert
            Assert.Null(nonMatchingCallSite);
        }

        [Fact]
        public void CreateCallSite_ReturnsService_IfClosedTypSatisfiesInterfaceGenericConstraint()
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var implementationType = typeof(TypeWithInterfaceConstraint<>);
            var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(descriptor);
            // Act
            var matchingType = typeof(IFakeOpenGenericService<string>);
            var matchingCallSite = callSiteFactory(matchingType);
            // Assert
            Assert.NotNull(matchingCallSite);
        }

        [Theory]
        [InlineData(typeof(IFakeOpenGenericService<int>), default(int), new[] { typeof(FakeOpenGenericService<int>), typeof(TypeWithStructConstraint<int>), typeof(TypeWithNewConstraint<int>) })]
        [InlineData(typeof(IFakeOpenGenericService<string>), "", new[] { typeof(FakeOpenGenericService<string>), typeof(TypeWithClassConstraint<string>), typeof(TypeWithInterfaceConstraint<string>) })]
        public void CreateCallSite_ReturnsMatchingTypesThatMatchCorrectConstraints(Type closedServiceType, object value, Type[] matchingImplementationTypes)
        {
            // Arrange
            var serviceType = typeof(IFakeOpenGenericService<>);
            var noConstraintImplementationType = typeof(FakeOpenGenericService<>);
            var noConstraintDescriptor = new ServiceDescriptor(serviceType, noConstraintImplementationType, ServiceLifetime.Transient);
            var structImplementationType = typeof(TypeWithStructConstraint<>);
            var structDescriptor = new ServiceDescriptor(serviceType, structImplementationType, ServiceLifetime.Transient);
            var classImplementationType = typeof(TypeWithClassConstraint<>);
            var classDescriptor = new ServiceDescriptor(serviceType, classImplementationType, ServiceLifetime.Transient);
            var newImplementationType = typeof(TypeWithNewConstraint<>);
            var newDescriptor = new ServiceDescriptor(serviceType, newImplementationType, ServiceLifetime.Transient);
            var interfaceImplementationType = typeof(TypeWithInterfaceConstraint<>);
            var interfaceDescriptor = new ServiceDescriptor(serviceType, interfaceImplementationType, ServiceLifetime.Transient);
            var serviceValueType = closedServiceType.GenericTypeArguments[0];
            var serviceValueDescriptor = new ServiceDescriptor(serviceValueType, value);
            var callSiteFactory = GetCallSiteFactory(noConstraintDescriptor, structDescriptor, classDescriptor, newDescriptor, interfaceDescriptor, serviceValueDescriptor);
            var collectionType = typeof(IEnumerable<>).MakeGenericType(closedServiceType);
            // Act
            var callSite = callSiteFactory(collectionType);
            // Assert
            var enumerableCall = Assert.IsType<IEnumerableCallSite>(callSite);
            Assert.Equal(matchingImplementationTypes.Length, enumerableCall.ServiceCallSites.Length);
            Assert.Equal(enumerableCall.ServiceCallSites.Select(scs => scs.ImplementationType).ToArray(), matchingImplementationTypes);
        }

        public static TheoryData CreateCallSite_PicksConstructorWithTheMostNumberOfResolvedParametersData =>
            new TheoryData<Type, Func<Type, ServiceCallSite>, Type[]>
            {
                {
                    typeof(TypeWithSupersetConstructors),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithSupersetConstructors), typeof(TypeWithSupersetConstructors), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService))
                    ),
                    new[] { typeof(IFakeService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithSupersetConstructors), typeof(TypeWithSupersetConstructors), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService))
                    ),
                    new[] { typeof(IFactoryService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithSupersetConstructors), typeof(TypeWithSupersetConstructors), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService))
                    ),
                    new[] { typeof(IFakeService), typeof(IFactoryService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithSupersetConstructors), typeof(TypeWithSupersetConstructors), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService))
                    ),
                    new[] { typeof(IFakeService), typeof(IFakeMultipleService), typeof(IFactoryService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithSupersetConstructors), typeof(TypeWithSupersetConstructors), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService)),
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService))
                    ),
                    new[] { typeof(IFakeMultipleService), typeof(IFactoryService), typeof(IFakeService), typeof(IFakeScopedService) }
                },
                {
                    typeof(TypeWithSupersetConstructors),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithSupersetConstructors), typeof(TypeWithSupersetConstructors), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService)),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(TransientFactoryService)),
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService))
                    ),
                    new[] { typeof(IFakeMultipleService), typeof(IFactoryService), typeof(IFakeService), typeof(IFakeScopedService) }
                },
                {
                    typeof(TypeWithGenericServices),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithGenericServices), typeof(TypeWithGenericServices), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFakeService), typeof(IFakeOpenGenericService<IFakeService>) }
                },
                {
                    typeof(TypeWithGenericServices),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithGenericServices), typeof(TypeWithGenericServices), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFakeService), typeof(IFactoryService), typeof(IFakeOpenGenericService<IFakeService>) }
                }
            };

        [Theory]
        [MemberData(nameof(CreateCallSite_PicksConstructorWithTheMostNumberOfResolvedParametersData))]
        private void CreateCallSite_PicksConstructorWithTheMostNumberOfResolvedParameters(
            Type type,
            Func<Type, ServiceCallSite> callSiteFactory,
            Type[] expectedConstructorParameters)
        {
            // Act
            var callSite = callSiteFactory(type);

            // Assert
            Assert.Equal(CallSiteResultCacheLocation.Dispose, callSite.Cache.Location);
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(expectedConstructorParameters, GetParameters(constructorCallSite));
        }

        public static TheoryData CreateCallSite_ConsidersConstructorsWithDefaultValuesData =>
            new TheoryData<Func<Type, object>, Type[]>
            {
                {
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithDefaultConstructorParameters), typeof(TypeWithDefaultConstructorParameters), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFakeMultipleService), typeof(IFakeService) }
                },
                {
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithDefaultConstructorParameters), typeof(TypeWithDefaultConstructorParameters), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFactoryService), typeof(IFakeScopedService) }
                },
                {
                   GetCallSiteFactory(
                       new ServiceDescriptor(typeof(TypeWithDefaultConstructorParameters), typeof(TypeWithDefaultConstructorParameters), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService), ServiceLifetime.Transient),
                        new ServiceDescriptor(typeof(IFactoryService), typeof(FakeService), ServiceLifetime.Transient)
                    ),
                    new[] { typeof(IFactoryService), typeof(IFakeScopedService) }
                }
            };

        [Theory]
        [MemberData(nameof(CreateCallSite_ConsidersConstructorsWithDefaultValuesData))]
        private void CreateCallSite_ConsidersConstructorsWithDefaultValues(
            Func<Type, ServiceCallSite> callSiteFactory,
            Type[] expectedConstructorParameters)
        {
            // Arrange
            var type = typeof(TypeWithDefaultConstructorParameters);

            // Act
            var callSite = callSiteFactory(type);

            // Assert
            Assert.Equal(CallSiteResultCacheLocation.Dispose, callSite.Cache.Location);
            var constructorCallSite = Assert.IsType<ConstructorCallSite>(callSite);
            Assert.Equal(expectedConstructorParameters, GetParameters(constructorCallSite));
        }

        [Fact]
        public void CreateCallSite_ThrowsIfTypeHasSingleConstructorWithUnresolvableParameters()
        {
            // Arrange
            var type = typeof(TypeWithParameterizedConstructor);
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);

            var callSiteFactory = GetCallSiteFactory(descriptor);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => callSiteFactory(type));
            Assert.Equal($"Unable to resolve service for type '{typeof(IFakeService)}' while attempting to activate '{type}'.",
                ex.Message);
        }

        [Theory]
        [InlineData(typeof(TypeWithMultipleParameterizedConstructors))]
        [InlineData(typeof(TypeWithSupersetConstructors))]
        public void CreateCallSite_ThrowsIfTypeHasNoConstructurWithResolvableParameters(Type type)
        {
            // Arrange
            var descriptor = new ServiceDescriptor(type, type, ServiceLifetime.Transient);
            var callSiteFactory = GetCallSiteFactory(
                descriptor,
                new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeScopedService), typeof(FakeService), ServiceLifetime.Transient)
            );

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => callSiteFactory(type));
            Assert.Equal($"No constructor for type '{type}' can be instantiated using services from the service container and default values.",
                ex.Message);
        }

        public static TheoryData CreateCallSite_ThrowsIfMultipleNonOverlappingConstructorsCanBeResolvedData =>
            new TheoryData<Type, Func<Type, object>, Type[][]>
            {
                {
                    typeof(TypeWithDefaultConstructorParameters),
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithDefaultConstructorParameters), typeof(TypeWithDefaultConstructorParameters), ServiceLifetime.Transient),
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
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithMultipleParameterizedConstructors), typeof(TypeWithMultipleParameterizedConstructors), ServiceLifetime.Transient),
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
                    GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithNonOverlappedConstructors), typeof(TypeWithNonOverlappedConstructors), ServiceLifetime.Transient),
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
                   GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithUnresolvableEnumerableConstructors), typeof(TypeWithUnresolvableEnumerableConstructors), ServiceLifetime.Transient),
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
                   GetCallSiteFactory(
                        new ServiceDescriptor(typeof(TypeWithUnresolvableEnumerableConstructors), typeof(TypeWithUnresolvableEnumerableConstructors), ServiceLifetime.Transient),
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
            Func<Type, object> callSiteFactory,
            Type[][] expectedConstructorParameterTypes)
        {
            // Arrange
            var expectedMessage =
                string.Join(
                    Environment.NewLine,
                    $"Unable to activate type '{type}'. The following constructors are ambiguous:",
                    GetConstructor(type, expectedConstructorParameterTypes[0]),
                    GetConstructor(type, expectedConstructorParameterTypes[1]));

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => callSiteFactory(type));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CreateCallSite_ThrowsIfMultipleNonOverlappingConstructorsForGenericTypesCanBeResolved()
        {
            // Arrange
            var type = typeof(TypeWithGenericServices);
            var expectedMessage = $"Unable to activate type '{type}'. The following constructors are ambiguous:";

            var callSiteFactory = GetCallSiteFactory(
                new ServiceDescriptor(type, type, ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeMultipleService), typeof(FakeService), ServiceLifetime.Transient),
                new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), ServiceLifetime.Transient)
            );

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => callSiteFactory(type));
            Assert.StartsWith(expectedMessage, ex.Message);
        }

        private static Func<Type, ServiceCallSite> GetCallSiteFactory(params ServiceDescriptor[] descriptors)
        {
            var collection = new ServiceCollection();
            foreach (var descriptor in descriptors)
            {
                collection.Add(descriptor);
            }

            var callSiteFactory = new CallSiteFactory(collection.ToArray());

            return type => callSiteFactory.GetCallSite(type, new CallSiteChain());
        }

        private static IEnumerable<Type> GetParameters(ConstructorCallSite constructorCallSite) =>
            constructorCallSite
                .ConstructorInfo
                .GetParameters()
                .Select(p => p.ParameterType);

        private static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes) =>
            type.GetTypeInfo().DeclaredConstructors.First(
                c => Enumerable.SequenceEqual(
                    c.GetParameters().Select(p => p.ParameterType),
                    parameterTypes));
    }
}
