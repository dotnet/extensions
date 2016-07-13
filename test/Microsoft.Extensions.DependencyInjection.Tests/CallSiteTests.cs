// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class CallSiteTests
    {
        private static readonly CallSiteRuntimeResolver CallSiteRuntimeResolver = new CallSiteRuntimeResolver();

        public static IEnumerable<object[]> TestServiceDescriptors(ServiceLifetime lifetime)
        {
            Func<object, object, bool> compare;

            if (lifetime == ServiceLifetime.Transient)
            {
                // Expect service references to be different for transient descriptors
                compare = (service1, service2) => service1 != service2;
            }
            else
            {
                // Expect service references to be the same for singleton and scoped descriptors
                compare = (service1, service2) => service1 == service2;
            }

            // Implementation Type Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), lifetime) },
                typeof(IFakeService),
                compare,
            };
            // Closed Generic Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeOpenGenericService<PocoClass>), typeof(FakeService), lifetime) },
                typeof(IFakeOpenGenericService<PocoClass>),
                compare,
            };
            // Open Generic Descriptor
            yield return new object[]
            {
                new[]
                {
                    new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), lifetime),
                    new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), lifetime),
                },
                typeof(IFakeOpenGenericService<IFakeService>),
                compare,
            };
            // Factory Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeService), _ => new FakeService(), lifetime) },
                typeof(IFakeService),
                compare,
            };

            if (lifetime == ServiceLifetime.Singleton)
            {
                // Instance Descriptor
                yield return new object[]
                {
                   new[] { new ServiceDescriptor(typeof(IFakeService), new FakeService()) },
                   typeof(IFakeService),
                   compare,
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestServiceDescriptors), ServiceLifetime.Singleton)]
        [MemberData(nameof(TestServiceDescriptors), ServiceLifetime.Scoped)]
        [MemberData(nameof(TestServiceDescriptors), ServiceLifetime.Transient)]
        public void BuiltExpressionWillReturnResolvedServiceWhenAppropriate(
            ServiceDescriptor[] desciptors, Type serviceType, Func<object, object, bool> compare)
        {
            var provider = new ServiceProvider(desciptors);

            var callSite = provider.GetServiceCallSite(serviceType, new HashSet<Type>());
            var collectionCallSite = provider.GetServiceCallSite(typeof(IEnumerable<>).MakeGenericType(serviceType), new HashSet<Type>());

            var compiledCallSite = CompileCallSite(callSite);
            var compiledCollectionCallSite = CompileCallSite(collectionCallSite);

            var service1 = Invoke(callSite, provider);
            var service2 = compiledCallSite(provider);
            var serviceEnumerator = ((IEnumerable)compiledCollectionCallSite(provider)).GetEnumerator();

            Assert.NotNull(service1);
            Assert.True(compare(service1, service2));

            // Service can be IEnumerable resolved. The IEnumerable should have exactly one element.
            Assert.True(serviceEnumerator.MoveNext());
            Assert.True(compare(service1, serviceEnumerator.Current));
            Assert.False(serviceEnumerator.MoveNext());
        }

        [Fact]
        public void BuiltExpressionCanResolveNestedScopedService()
        {
            var descriptors = new ServiceCollection();
            descriptors.AddScoped<ServiceA>();
            descriptors.AddScoped<ServiceB>();
            descriptors.AddScoped<ServiceC>();

            var provider = new ServiceProvider(descriptors);
            var callSite = provider.GetServiceCallSite(typeof(ServiceC), new HashSet<Type>());
            var compiledCallSite = CompileCallSite(callSite);

            var serviceC = (ServiceC)compiledCallSite(provider);

            Assert.NotNull(serviceC.ServiceB.ServiceA);
            Assert.Equal(serviceC, Invoke(callSite, provider));
        }

        [Fact]
        public void BuiltExpressionRethrowsOriginalExceptionFromConstructor()
        {
            var descriptors = new ServiceCollection();
            descriptors.AddTransient<ClassWithThrowingEmptyCtor>();
            descriptors.AddTransient<ClassWithThrowingCtor>();
            descriptors.AddTransient<IFakeService, FakeService>();

            var provider = new ServiceProvider(descriptors);

            var callSite1 = provider.GetServiceCallSite(typeof(ClassWithThrowingEmptyCtor), new HashSet<Type>());
            var compiledCallSite1 = CompileCallSite(callSite1);

            var callSite2 = provider.GetServiceCallSite(typeof(ClassWithThrowingCtor), new HashSet<Type>());
            var compiledCallSite2 = CompileCallSite(callSite2);

            var ex1 = Assert.Throws<Exception>(() => compiledCallSite1(provider));
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);

            var ex2 = Assert.Throws<Exception>(() => compiledCallSite2(provider));
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }

        private class ServiceA
        {
        }

        private class ServiceB
        {
            public ServiceB(ServiceA serviceA)
            {
                ServiceA = serviceA;
            }

            public ServiceA ServiceA { get; set; }
        }

        private class ServiceC
        {
            public ServiceC(ServiceB serviceB)
            {
                ServiceB = serviceB;
            }

            public ServiceB ServiceB { get; set; }
        }

        private static object Invoke(IServiceCallSite callSite, ServiceProvider provider)
        {
            return CallSiteRuntimeResolver.Resolve(callSite, provider);
        }

        private static Func<ServiceProvider, object> CompileCallSite(IServiceCallSite callSite)
        {
            return new CallSiteExpressionBuilder(CallSiteRuntimeResolver).Build(callSite);
        }
    }
}