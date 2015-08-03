// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.DependencyInjection.Tests.Fakes;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
{
    public class TypeWithMultipleParameterizedConstructors
    {
        public TypeWithMultipleParameterizedConstructors(IFactoryService factoryService)
        {
        }

        public TypeWithMultipleParameterizedConstructors(IFakeService fakeService)
        {
        }
    }
}
