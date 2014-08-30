// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using Xunit;

namespace Microsoft.Framework.DependencyInjection
{
    public class ServiceDescriberTests
    {
        public delegate ServiceDescriptor DescribeAction(ServiceDescriber desciber);

        public static TheoryData ServiceDescriberActions
        {
            get
            {
                var theoryData = new TheoryData<DescribeAction, LifecycleKind>();
                theoryData.Add(describer => describer.Describe(typeof(IFakeService), typeof(FakeService), LifecycleKind.Transient), LifecycleKind.Transient);
                theoryData.Add(describer => describer.Transient(typeof(IFakeService), typeof(FakeService)), LifecycleKind.Transient);
                theoryData.Add(describer => describer.Scoped(typeof(IFakeService), typeof(FakeService)), LifecycleKind.Scoped);
                theoryData.Add(describer => describer.Singleton(typeof(IFakeService), typeof(FakeService)), LifecycleKind.Singleton);

                theoryData.Add(describer => describer.Instance(typeof(IFakeService), new FakeService()), LifecycleKind.Singleton);

                Func<IServiceProvider, object> _factory = service => new FakeService();
                theoryData.Add(describer => describer.Describe(typeof(IFakeService), _factory, LifecycleKind.Transient), LifecycleKind.Transient);
                theoryData.Add(describer => describer.Transient(typeof(IFakeService), _factory), LifecycleKind.Transient);
                theoryData.Add(describer => describer.Scoped(typeof(IFakeService), _factory), LifecycleKind.Scoped);
                theoryData.Add(describer => describer.Singleton(typeof(IFakeService), _factory), LifecycleKind.Singleton);

                return theoryData;
            }
        }

        [Theory]
        [MemberData(nameof(ServiceDescriberActions))]
        public void Descriptor_ReplacesServicesFromConfiguration(DescribeAction action, LifecycleKind lifeCycle)
        {
            // Arrange
            var configuration = new Configuration();
            configuration.Add(new MemoryConfigurationSource());
            configuration.Set(typeof(IFakeService).FullName, typeof(FakeOuterService).AssemblyQualifiedName);
            var serviceDescriber = new ServiceDescriber(configuration);

            // Act
            var descriptor = action(serviceDescriber);

            // Assert
            Assert.Equal(typeof(IFakeService), descriptor.ServiceType);
            Assert.Equal(typeof(FakeOuterService), descriptor.ImplementationType);
            Assert.Equal(lifeCycle, descriptor.Lifecycle);
            Assert.Null(descriptor.ImplementationFactory);
            Assert.Null(descriptor.ImplementationInstance);
        }

        [Theory]
        [MemberData(nameof(ServiceDescriberActions))]
        public void Descriptor_ThrowsIfReplacedServiceCanotBeFound(DescribeAction action, LifecycleKind lifeCycle)
        {
            // Arrange
            var expected = "TODO: unable to locate implementation Type-Does-NotExist for service " + typeof(IFakeService).FullName;
            var configuration = new Configuration();
            configuration.Add(new MemoryConfigurationSource());
            configuration.Set(typeof(IFakeService).FullName, "Type-Does-NotExist");
            var serviceDescriber = new ServiceDescriber(configuration);

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() => action(serviceDescriber));
            Assert.Equal(expected, ex.Message);
        }
    }
}