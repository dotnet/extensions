// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Distributed.VSRC1;
using Microsoft.Extensions.Caching.Memory.VSRC1;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection.VSRC1
{
    /// <summary>
    /// Extension methods for setting up memory cache related services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class MemoryCacheServicesExtensions
    {
        /// <summary>
        /// Adds memory caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddCaching(this IServiceCollection services)
        {
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Transient<IDistributedCache, LocalCache>());
            services.TryAdd(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());
            return services;
        }
    }
}