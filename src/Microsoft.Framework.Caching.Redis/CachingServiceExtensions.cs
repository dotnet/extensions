// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Redis;
namespace Microsoft.Framework.DependencyInjection
{
    public static class CachingServicesExtensions
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection collection)
        {
            collection.AddOptions();
            collection.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, RedisCache>());
            return collection;
        }
    }
}
