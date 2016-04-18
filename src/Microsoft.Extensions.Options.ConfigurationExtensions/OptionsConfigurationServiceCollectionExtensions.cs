// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding configuration related options services to the DI container.
    /// </summary>
    public static class OptionsConfigurationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config)
            where TOptions : class
        {
            return services.Configure<TOptions>(config, trackConfigChanges: false);
        }

        /// <summary>
        /// Registers a configuration instance which TOptions will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The type of options being configured.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="config">The configuration being bound.</param>
        /// <param name="trackConfigChanges">If true, registers an change token source for use by an IOptionsMonitor.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config, bool trackConfigChanges)
            where TOptions : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddSingleton<IConfigureOptions<TOptions>>(new ConfigureFromConfigurationOptions<TOptions>(config));
            if (trackConfigChanges)
            {
                services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(new ConfigurationChangeTokenSource<TOptions>(config));
            }
            return services;
        }
    }
}