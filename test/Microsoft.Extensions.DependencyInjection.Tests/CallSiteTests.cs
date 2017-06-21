// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
            ServiceDescriptor[] descriptors, Type serviceType, Func<object, object, bool> compare)
        {
            var provider = new ServiceProvider(descriptors, new ServiceProviderOptions { ValidateScopes = true });

            var callSite = provider.CallSiteFactory.CreateCallSite(serviceType, new HashSet<Type>());
            var collectionCallSite = provider.CallSiteFactory.CreateCallSite(typeof(IEnumerable<>).MakeGenericType(serviceType), new HashSet<Type>());

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

            var provider = new ServiceProvider(descriptors, new ServiceProviderOptions { ValidateScopes = true });
            var callSite = provider.CallSiteFactory.CreateCallSite(typeof(ServiceC), new HashSet<Type>());
            var compiledCallSite = CompileCallSite(callSite);

            var serviceC = (ServiceC)compiledCallSite(provider);

            Assert.NotNull(serviceC.ServiceB.ServiceA);
            Assert.Equal(serviceC, Invoke(callSite, provider));
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        // We are not testing singleton here because singleton resolutions always got through
        // runtime resolver and there is no sense to eliminating call from there
        public void BuildExpressionElidesDisposableCaptureForNonDisposableServices(ServiceLifetime lifetime)
        {
            IServiceCollection descriptors = new ServiceCollection();
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceB), typeof(ServiceB), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceC), typeof(ServiceC), lifetime));

            descriptors.AddScoped<ServiceB>();
            descriptors.AddTransient<ServiceC>();

            var disposables = new List<object>();
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            provider._captureDisposableCallback = obj =>
            {
                disposables.Add(obj);
            };
            var callSite = provider.CallSiteFactory.CreateCallSite(typeof(ServiceC), new HashSet<Type>());
            var compiledCallSite = CompileCallSite(callSite);

            var serviceC = (ServiceC)compiledCallSite(provider);

            Assert.Equal(0, disposables.Count);
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        // We are not testing singleton here because singleton resolutions always got through
        // runtime resolver and there is no sense to eliminating call from there
        public void BuildExpressionElidesDisposableCaptureForEnumerableServices(ServiceLifetime lifetime)
        {
            IServiceCollection descriptors = new ServiceCollection();
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceA), typeof(ServiceA), lifetime));
            descriptors.Add(ServiceDescriptor.Describe(typeof(ServiceD), typeof(ServiceD), lifetime));

            var disposables = new List<object>();
            var provider = new ServiceProvider(descriptors, ServiceProviderOptions.Default);
            provider._captureDisposableCallback = obj =>
            {
                disposables.Add(obj);
            };
            var callSite = provider.CallSiteFactory.CreateCallSite(typeof(ServiceD), new HashSet<Type>());
            var compiledCallSite = CompileCallSite(callSite);

            var serviceD = (ServiceD)compiledCallSite(provider);

            Assert.Equal(0, disposables.Count);
        }

        [Fact]
        public void BuiltExpressionRethrowsOriginalExceptionFromConstructor()
        {
            var descriptors = new ServiceCollection();
            descriptors.AddTransient<ClassWithThrowingEmptyCtor>();
            descriptors.AddTransient<ClassWithThrowingCtor>();
            descriptors.AddTransient<IFakeService, FakeService>();

            var provider = new ServiceProvider(descriptors, new ServiceProviderOptions { ValidateScopes = true });

            var callSite1 = provider.CallSiteFactory.CreateCallSite(typeof(ClassWithThrowingEmptyCtor), new HashSet<Type>());
            var compiledCallSite1 = CompileCallSite(callSite1);

            var callSite2 = provider.CallSiteFactory.CreateCallSite(typeof(ClassWithThrowingCtor), new HashSet<Type>());
            var compiledCallSite2 = CompileCallSite(callSite2);

            var ex1 = Assert.Throws<Exception>(() => compiledCallSite1(provider));
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);

            var ex2 = Assert.Throws<Exception>(() => compiledCallSite2(provider));
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }

        private class ServiceD
        {
            public ServiceD(IEnumerable<ServiceA> services)
            {

            }
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
