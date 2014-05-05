// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
