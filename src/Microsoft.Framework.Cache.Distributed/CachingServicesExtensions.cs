// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Cache.Distributed;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class CachingServicesExtensions
    {
        public static IServiceCollection AddCachingServices(this IServiceCollection collection, IConfiguration configuration = null)
        {
            collection.AddOptions(configuration);
            return collection.AddTransient<IDistributedCache, LocalCache>()
                .AddSingleton<IMemoryCache, MemoryCache>();
        }
    }
}