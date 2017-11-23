// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IHttpClientBuilder"/>
    /// </summary>
    public static class HttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configureClient">A delegate that is used to configure an <see cref="HttpClient"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder ConfigureHttpClient(this IHttpClientBuilder builder, Action<HttpClient> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.HttpClientActions.Add(configureClient));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to create an additional message handler for a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configureHandler">A delegate that is used to create a <see cref="DelegatingHandler"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// The <see paramref="configureHandler"/> delegate should return a new instance of the message handler each time it
        /// is invoked.
        /// </remarks>
        public static IHttpClientBuilder AddHttpMessageHandler(this IHttpClientBuilder builder, Func<DelegatingHandler> configureHandler)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureHandler == null)
            {
                throw new ArgumentNullException(nameof(configureHandler));
            }

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b => b.AdditionalHandlers.Add(configureHandler()));
            });

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to create an additional message handler for a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configureHandler">A delegate that is used to create a <see cref="DelegatingHandler"/>.</param>       /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// The <see paramref="configureHandler"/> delegate should return a new instance of the message handler each time it
        /// is invoked.
        /// </remarks>
        public static IHttpClientBuilder AddHttpMessageHandler(this IHttpClientBuilder builder, Func<IServiceProvider, DelegatingHandler> configureHandler)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureHandler == null)
            {
                throw new ArgumentNullException(nameof(configureHandler));
            }

            builder.Services.AddTransient<IConfigureOptions<HttpClientFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<HttpClientFactoryOptions>(builder.Name, (options) =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.AdditionalHandlers.Add(configureHandler(services)));
                });
            });

            return builder;
        }

        /// <summary>
        /// Adds an additional message handler from the dependency injection container for a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <typeparam name="THandler">
        /// The type of the <see cref="DelegatingHandler"/>. The handler type must be registered as a transient service.
        /// </typeparam>
        public static IHttpClientBuilder AddHttpMessageHandler<THandler>(this IHttpClientBuilder builder)
            where THandler : DelegatingHandler
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddTransient<IConfigureOptions<HttpClientFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<HttpClientFactoryOptions>(builder.Name, (options) =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.AdditionalHandlers.Add(services.GetRequiredService<THandler>()));
                });
            });

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure the primary <see cref="HttpMessageHandler"/> for a 
        /// named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configureHandler">A delegate that is used to create an <see cref="HttpMessageHandler"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// The <see paramref="configureHandler"/> delegate should return a new instance of the message handler each time it
        /// is invoked.
        /// </remarks>
        public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this IHttpClientBuilder builder, Func<HttpMessageHandler> configureHandler)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureHandler == null)
            {
                throw new ArgumentNullException(nameof(configureHandler));
            }

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = configureHandler());
            });

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure the primary <see cref="HttpMessageHandler"/> for a 
        /// named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configureHandler">A delegate that is used to create an <see cref="HttpMessageHandler"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// The <see paramref="configureHandler"/> delegate should return a new instance of the message handler each time it
        /// is invoked.
        /// </remarks>
        public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this IHttpClientBuilder builder, Func<IServiceProvider, HttpMessageHandler> configureHandler)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureHandler == null)
            {
                throw new ArgumentNullException(nameof(configureHandler));
            }

            builder.Services.AddTransient<IConfigureOptions<HttpClientFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<HttpClientFactoryOptions>(builder.Name, (options) =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = configureHandler(services));
                });
            });

            return builder;
        }

        /// <summary>
        /// Configures the primary <see cref="HttpMessageHandler"/> from the dependency inection container
        /// for a  named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <typeparam name="THandler">
        /// The type of the <see cref="DelegatingHandler"/>. The handler type must be registered as a transient service.
        /// </typeparam>
        public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(this IHttpClientBuilder builder)
            where THandler : HttpMessageHandler
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddTransient<IConfigureOptions<HttpClientFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<HttpClientFactoryOptions>(builder.Name, (options) =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = services.GetRequiredService<THandler>());
                });
            });

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure message handlers using <see cref="HttpMessageHandlerBuilder"/> 
        /// for a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="configureBuilder">A delegate that is used to configure an <see cref="HttpMessageHandlerBuilder"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder ConfigureHttpMessageHandlerBuilder(this IHttpClientBuilder builder, Action<HttpMessageHandlerBuilder> configureBuilder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureBuilder == null)
            {
                throw new ArgumentNullException(nameof(configureBuilder));
            }

            builder.Services.Configure<HttpClientFactoryOptions>(builder.Name, options => options.HttpMessageHandlerBuilderActions.Add(configureBuilder));

            return builder;
        }

        /// <summary>
        /// Configures a binding between the <typeparamref name="TClient" /> type and the named <see cref="HttpClient"/>
        /// associated with the <see cref="IHttpClientBuilder"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <remarks>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Calling <see cref="HttpClientBuilderExtensions.AddTypedClient{TClient}(IHttpClientBuilder)"/> will register a typed
        /// client binding that creates <typeparamref name="TClient"/> using the <see cref="ITypedHttpClientFactory" />.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder)
            where TClient : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddTransient<TClient>(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);

                var typedClientFactory = s.GetRequiredService<ITypedHttpClientFactory>();
                return typedClientFactory.CreateClient<TClient>(httpClient);
            });

            return builder;
        }

        /// <summary>
        /// Configures a binding between the <typeparamref name="TClient" /> type and the named <see cref="HttpClient"/>
        /// associated with the <see cref="IHttpClientBuilder"/>. The created instances will be of type 
        /// <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The declared type of the typed client. They type specified will be registered in the service collection as
        /// a transient service. See <see cref="ITypedHttpClientFactory" /> for more details about authoring typed clients.
        /// </typeparam>
        /// <typeparam name="TImplementation">
        /// The implementation type of the typed client. The type specified by will be instantiated by the 
        /// <see cref="ITypedHttpClientFactory"/>.
        /// </typeparam>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <remarks>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Calling <see cref="HttpClientBuilderExtensions.AddTypedClient{TClient,TImplementation}(IHttpClientBuilder)"/>
        /// will register a typed client binding that creates <typeparamref name="TImplementation"/> using the 
        /// <see cref="ITypedHttpClientFactory" />.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddTypedClient<TClient, TImplementation>(this IHttpClientBuilder builder)
            where TClient : class
            where TImplementation : class, TClient
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddTransient<TClient>(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);

                var typedClientFactory = s.GetRequiredService<ITypedHttpClientFactory>();
                return typedClientFactory.CreateClient<TImplementation>(httpClient);
            });

            return builder;
        }

        /// <summary>
        /// Configures a binding between the <typeparamref name="TClient" /> type and the named <see cref="HttpClient"/>
        /// associated with the <see cref="IHttpClientBuilder"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service.
        /// </typeparam>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="factory">A factory function that will be used to construct the typed client.</param>
        /// <remarks>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Calling <see cref="HttpClientBuilderExtensions.AddTypedClient{TClient}(IHttpClientBuilder,Func{HttpClient,TClient})"/>
        /// will register a typed client binding that creates <typeparamref name="TClient"/> using the provided factory function.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder, Func<HttpClient, TClient> factory)
            where TClient : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddTransient<TClient>(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);

                return factory(httpClient);
            });

            return builder;
        }

        /// <summary>
        /// Configures a binding between the <typeparamref name="TClient" /> type and the named <see cref="HttpClient"/>
        /// associated with the <see cref="IHttpClientBuilder"/>.
        /// </summary>
        /// <typeparam name="TClient">
        /// The type of the typed client. They type specified will be registered in the service collection as
        /// a transient service.
        /// </typeparam>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
        /// <param name="factory">A factory function that will be used to construct the typed client.</param>
        /// <remarks>
        /// <para>
        /// <typeparamref name="TClient"/> instances constructed with the appropriate <see cref="HttpClient" />
        /// can be retrieved from <see cref="IServiceProvider.GetService(Type)" /> (and related methods) by providing
        /// <typeparamref name="TClient"/> as the service type. 
        /// </para>
        /// <para>
        /// Calling <see cref="HttpClientBuilderExtensions.AddTypedClient{TClient}(IHttpClientBuilder,Func{HttpClient,IServiceProvider,TClient})"/>
        /// will register a typed client binding that creates <typeparamref name="TClient"/> using the provided factory function.
        /// </para>
        /// </remarks>
        public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder, Func<HttpClient, IServiceProvider, TClient> factory)
            where TClient : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddTransient<TClient>(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);

                return factory(httpClient, s);
            });

            return builder;
        }
    }
}
