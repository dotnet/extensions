// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class CallSiteTests
    {
        public static IEnumerable<object[]> TestServiceDescriptors(LifecycleKind lifecycle)
        {
            Func<object, object, bool> compare;

            if (lifecycle == LifecycleKind.Transient)
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
                new[] { new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), lifecycle) },
                typeof(IFakeService),
                compare,
            };
            // Closed Generic Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeOpenGenericService<string>), typeof(FakeService), lifecycle) },
                typeof(IFakeOpenGenericService<string>),
                compare,
            };
            // Open Generic Descriptor
            yield return new object[]
            {
                new[]
                {
                    new ServiceDescriptor(typeof(IFakeService), typeof(FakeService), lifecycle),
                    new ServiceDescriptor(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>), lifecycle),
                },
                typeof(IFakeOpenGenericService<IFakeService>),
                compare,
            };
            // Factory Descriptor
            yield return new object[]
            {
                new[] { new ServiceDescriptor(typeof(IFakeService), _ => new FakeService(), lifecycle) },
                typeof(IFakeService),
                compare,
            };

            if (lifecycle == LifecycleKind.Singleton)
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
        [MemberData(nameof(TestServiceDescriptors), LifecycleKind.Singleton)]
        [MemberData(nameof(TestServiceDescriptors), LifecycleKind.Scoped)]
        [MemberData(nameof(TestServiceDescriptors), LifecycleKind.Transient)]
        public void BuiltExpressionWillReturnResolvedServiceWhenAppropriate(
            IServiceDescriptor[] desciptors, Type serviceType, Func<object, object, bool> compare)
        {
            var provider = new ServiceProvider(desciptors);

            var callSite = provider.GetServiceCallSite(serviceType, new HashSet<Type>());
            var collectionCallSite = provider.GetServiceCallSite(typeof(IEnumerable<>).MakeGenericType(serviceType), new HashSet<Type>());

            var compiledCallSite = CompileCallSite(callSite);
            var compiledCollectionCallSite = CompileCallSite(collectionCallSite);

            var service1 = callSite.Invoke(provider);
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
            Assert.Equal(serviceC, callSite.Invoke(provider));
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

        private static Func<ServiceProvider, object> CompileCallSite(IServiceCallSite callSite)
        {
            var providerExpression = Expression.Parameter(typeof(ServiceProvider), "provider");

            var lambdaExpression = Expression.Lambda<Func<ServiceProvider, object>>(
                callSite.Build(providerExpression),
                providerExpression);

            return lambdaExpression.Compile();
        }
    }
}