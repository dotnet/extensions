// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.SqlServer;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up Microsoft SQL Server distributed cache related services in an
    /// <see cref="IServiceCollection" />.
    /// </summary>
    public static class SqlServerCachingServicesExtensions
    {
        /// <summary>
        /// Adds Microsoft SQL Server distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="options">An action callback to configure a <see cref="SqlServerCacheOptions" /> instance.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns> 
        public static IServiceCollection AddSqlServerCache(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<SqlServerCacheOptions> options)
        {
            services.AddOptions();
            AddSqlServerCacheServices(services);
            services.Configure(options);
            return services;
        }

        // to enable unit testing
        internal static void AddSqlServerCacheServices(IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, SqlServerCache>());
        }
    }
}