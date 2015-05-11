// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;

namespace Microsoft.Framework.DependencyInjection
{
    public static class CachingServicesExtensions
    {
        public static IServiceCollection AddCaching(this IServiceCollection collection)
        {
            collection.AddOptions();
            collection.TryAdd(ServiceDescriptor.Transient<IDistributedCache, LocalCache>());
            collection.TryAdd(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());
            return collection;
        }
    }
}