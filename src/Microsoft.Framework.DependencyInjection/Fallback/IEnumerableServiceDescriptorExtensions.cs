// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.DependencyInjection.Fallback
{
    public static class IEnumerableServiceDescriptorExtensions
    {
        public static IServiceProvider BuildServiceProvider(this IEnumerable<IServiceDescriptor> services)
        {
            return new ServiceProvider(services);
        }
    }
}
