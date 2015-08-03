// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection.Tests.Fakes;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
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
