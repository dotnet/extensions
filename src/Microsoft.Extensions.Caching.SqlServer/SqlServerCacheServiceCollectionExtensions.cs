// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.SqlServer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up Microsoft SQL Server distributed cache services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class SqlServerCachingServicesExtensions
    {
        /// <summary>
        /// Adds Microsoft SQL Server distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{SqlServerCacheOptions}"/> to configure the provided <see cref="SqlServerCacheOptions"/>.</param>
        public static void AddDistributedSqlServerCache(this IServiceCollection services, Action<SqlServerCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            AddSqlServerCacheServices(services);
            services.Configure(setupAction);
        }

        // to enable unit testing
        internal static void AddSqlServerCacheServices(IServiceCollection services)
        {
            services.Add(ServiceDescriptor.Singleton<IDistributedCache, SqlServerCache>());
        }
    }
}