// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET8_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Telemetry.Http.Logging;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods to register the HTTP logging feature within the service.
/// </summary>
public static class HttpLoggingServiceExtensions
{
    /// <summary>
    /// Enables redaction of HTTP logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureRedaction">Configures the redaction options.</param>
    /// <param name="configureLogging">Configures the logging options.</param>
    /// <returns>The original service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    [Experimental("ID")]
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, Action<LoggingRedactionOptions>? configureRedaction = null,
        Action<HttpLoggingOptions>? configureLogging = null)
    {
        _ = Throw.IfNull(services);

        var builder = services
            .AddOptionsWithValidateOnStart<LoggingRedactionOptions, LoggingRedactionOptionsValidator>();

        _ = services.Configure(configureRedaction ?? (static _ => { }));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpLoggingInterceptor, HttpLoggingRedactionInterceptor>());

        _ = services.AddHttpLogging(o =>
        {
            o.CombineLogs = true;
            configureLogging?.Invoke(o);
        });

        // Internal stuff for route processing:
        _ = services.AddHttpRouteProcessor();
        _ = services.AddHttpRouteUtilities();
        return services;
    }

    /// <summary>
    /// Enables enrichment of HTTP logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The original service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    [Experimental("ID")]
    public static IServiceCollection AddHttpLoggingEnrichment(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpLoggingInterceptor, HttpLoggingEnrichmentInterceptor>());

        return services;
    }

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T"/> to the <see cref="IServiceCollection"/> to enrich incoming HTTP requests logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the instance of <typeparamref name="T"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services)
        where T : class, IHttpLogEnricher
    {
        _ = Throw.IfNull(services);

        services.AddHttpLoggingEnrichment();

        return services.AddActivatedSingleton<IHttpLogEnricher, T>();
    }
}
#endif
