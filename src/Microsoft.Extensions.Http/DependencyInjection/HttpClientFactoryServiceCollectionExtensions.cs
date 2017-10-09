// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions methods to configure an <see cref="IServiceCollection"/> for <see cref="IHttpClientFactory"/>.
    /// </summary>
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddHttpClient(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.AddTransient<HttpMessageHandlerBuilder, DefaultHttpMessageHandlerBuilder>();
            services.AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();
            return services;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddHttpClient(this IServiceCollection services, string name, Action<HttpClient> configureClient)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddHttpClient(services);
            services.Configure<HttpClientFactoryOptions>(name, options => options.HttpClientActions.Add(configureClient));

            return services;
        }
    }
}