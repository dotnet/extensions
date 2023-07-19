﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Extension methods to register HTTP client logging feature.
/// </summary>
public static class HttpClientLoggingExtensions
{
    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">Argument <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        _ = services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return services.ConfigureHttpClientDefaults(
            static httpClientBuilder =>
                httpClientBuilder
                .RemoveAllLoggers()
                .AddLogger(
                    wrapHandlersPipeline: true,
                    httpClientLoggerFactory: static serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<HttpLoggingHandler>>();
                        var httpRequestReader = serviceProvider.GetRequiredService<IHttpRequestReader>();
                        var enrichers = serviceProvider.GetServices<IHttpClientLogEnricher>();
                        var loggingOptions = serviceProvider.GetRequiredService<IOptions<LoggingOptions>>();

                        return new HttpClientLogger(logger, httpRequestReader, enrichers, loggingOptions);
                    }));
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="LoggingOptions"/>.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>()
            .Bind(section);

        return services.AddDefaultHttpClientLogging();
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="LoggingOptions"/> with.</param>
    /// <returns>
    /// <see cref="IServiceCollection" /> instance for chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>()
            .Configure(configure);

        return services.AddDefaultHttpClientLogging();
    }

    /// <summary>
    /// Registers HTTP client logging components into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="ArgumentNullException">Argument <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>(builder.Name);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        builder.Services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        builder.Services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return builder.RemoveAllLoggers()
            .AddLoggerInternal();
    }

    /// <summary>
    /// Registers HTTP client logging components into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="LoggingOptions"/>.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>(builder.Name)
            .Bind(section);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        builder.Services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        builder.Services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return builder.RemoveAllLoggers()
            .AddLoggerInternal();
    }

    /// <summary>
    /// Registers HTTP client logging components into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder" />.</param>
    /// <param name="configure">The delegate to configure <see cref="LoggingOptions"/> with.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>(builder.Name)
            .Configure(configure);

        _ = builder.Services
            .AddHttpRouteProcessor()
            .AddHttpHeadersRedactor()
            .AddOutgoingRequestContext();

        builder.Services.TryAddActivatedSingleton<IHttpRequestReader, HttpRequestReader>();
        builder.Services.TryAddActivatedSingleton<IHttpHeadersReader, HttpHeadersReader>();

        return builder.RemoveAllLoggers()
            .AddLoggerInternal();
    }

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T"/> to the <see cref="IServiceCollection"/> to enrich HTTP client logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the instance of <typeparamref name="T"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services)
        where T : class, IHttpClientLogEnricher
    {
        _ = Throw.IfNull(services);

        _ = services.AddActivatedSingleton<IHttpClientLogEnricher, T>();

        return services;
    }

    private static IHttpClientBuilder AddLoggerInternal(this IHttpClientBuilder builder, bool wrapHandlersPipeline = true)
        => builder.AddLogger(
            serviceProvider =>
            {
                var loggingOptions = Options.Options.Create(serviceProvider
                    .GetRequiredService<IOptionsMonitor<LoggingOptions>>().Get(builder.Name));

                // We can do it the following way instead:
                /*
                    return ActivatorUtilities.CreateInstance<HttpLoggingHandler>(
                        serviceProvider,
                        ActivatorUtilities.CreateInstance<HttpRequestReader>(
                            serviceProvider,
                            ActivatorUtilities.CreateInstance<HttpHeadersReader>(
                                serviceProvider,
                                loggingOptions),
                            loggingOptions),
                        loggingOptions)
                */

                var logger = serviceProvider.GetRequiredService<ILogger<HttpLoggingHandler>>();
                var httpHeadersReader = new HttpHeadersReader(loggingOptions, serviceProvider.GetRequiredService<IHttpHeadersRedactor>());
                var httpRequestReader = new HttpRequestReader(
                    loggingOptions,
                    serviceProvider.GetRequiredService<IHttpRouteFormatter>(),
                    httpHeadersReader,
                    serviceProvider.GetRequiredService<IOutgoingRequestContext>(),
                    serviceProvider.GetService<IDownstreamDependencyMetadataManager>());

                var enrichers = serviceProvider.GetServices<IHttpClientLogEnricher>();

                return new HttpClientLogger(logger, httpRequestReader, enrichers, loggingOptions);
            },
            wrapHandlersPipeline);
}
