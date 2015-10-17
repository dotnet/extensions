// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithSupersetConstructors
    {
        public TypeWithSupersetConstructors(IFactoryService factoryService)
        {
        }

        public TypeWithSupersetConstructors(IFakeService fakeService)
        {
        }

        public TypeWithSupersetConstructors(
            IFakeService fakeService, 
            IFactoryService factoryService)
        {
        }

        public TypeWithSupersetConstructors(
           IFakeService fakeService,
           IFakeMultipleService multipleService,
           IFactoryService factoryService)
        {
        }

        public TypeWithSupersetConstructors(
           IFakeMultipleService multipleService, 
           IFactoryService factoryService,
           IFakeService fakeService,
           IFakeScopedService scopedService)
        {
        }
    }
}
