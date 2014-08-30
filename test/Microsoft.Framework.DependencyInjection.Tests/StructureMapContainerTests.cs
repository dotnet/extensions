// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection.Tests.Fakes;
using StructureMap;

namespace Microsoft.Framework.DependencyInjection.Tests
{
    public class StructureMapContainerTests : AllContainerTestsBase
    {
        protected override IServiceProvider CreateContainer()
        {
            var container = new Container(builder =>
            {
                foreach (var descriptor in TestServices.DefaultServices())
                {
                    if (descriptor.ImplementationType != null)
                    {
                        builder.For(descriptor.ServiceType).Use(descriptor.ImplementationType);
                    }
                    else if (descriptor.ImplementationFactory != null)
                    {
                        builder.For(descriptor.ServiceType)
                               .Use(context =>
                               {
                                   var provider = context.GetInstance<IServiceProvider>();
                                   return descriptor.ImplementationFactory(provider);
                               });
                    }
                    else
                    {
                        builder.For(descriptor.ServiceType).Use(descriptor.ImplementationInstance);
                    }
                }

                builder.For<IServiceProvider>().Use<StructureMapServiceProvider>();
            });

            return container.GetInstance<IServiceProvider>();
        }

        private class StructureMapServiceProvider : IServiceProvider
        {
            private readonly IContainer _container;

            public StructureMapServiceProvider(IContainer container)
            {
                _container = container;
            }

            public object GetService(Type type)
            {
                return _container.TryGetInstance(type) ?? GetMultiService(type);
            }

            private object GetMultiService(Type collectionType)
            {
                return MultiServiceHelpers.GetMultiService(collectionType,
                    serviceType => _container.GetAllInstances(serviceType));
            }
        }
    }
}
