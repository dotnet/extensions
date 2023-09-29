// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register the HTTP logging feature within the service.
/// </summary>
[Experimental(diagnosticId: Experiments.HttpLogging, UrlFormat = Experiments.UrlFormat)]
public static class HttpLoggingServiceExtensions
{
    /// <summary>
    /// Enables redaction of HTTP logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureRedaction">Configures the redaction options.</param>
    /// <returns>The original service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, Action<LoggingRedactionOptions>? configureRedaction = null)
    {
        _ = Throw.IfNull(services);

        _ = services.Configure(configureRedaction ?? (static _ => { }));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpLoggingInterceptor, HttpLoggingRedactionInterceptor>());

        _ = services.AddHttpLogging(o =>
        {
            o.CombineLogs = true;
            o.LoggingFields |= HttpLoggingFields.Duration;
        });

        // Internal stuff for route processing:
        _ = services.AddHttpRouteProcessor();
        _ = services.AddHttpRouteUtilities();
        return services;
    }

    /// <summary>
    /// Enables redaction of HTTP logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="section">The configuration section with the redaction settings.</param>
    /// <returns>The original service collection.</returns>
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, IConfigurationSection section)
    {
        return services.AddHttpLoggingRedaction(configureRedaction: o =>
        {
            o.RequestPathLoggingMode = section.GetSection(nameof(LoggingRedactionOptions.RequestPathLoggingMode)).Get<IncomingPathLoggingMode>();
            o.RequestPathParameterRedactionMode = section.GetSection(nameof(LoggingRedactionOptions.RequestPathParameterRedactionMode)).Get<HttpRouteParameterRedactionMode>();
            var paths = section.GetSection(nameof(LoggingRedactionOptions.ExcludePathStartsWith)).Get<string[]>();
            if (paths != null)
            {
                foreach (var path in paths)
                {
                    _ = o.ExcludePathStartsWith.Add(path);
                }
            }

            var routeParams = section.GetSection(nameof(LoggingRedactionOptions.RouteParameterDataClasses)).Get<Dictionary<string, DataClassification>>();
            if (routeParams != null)
            {
                foreach (var param in routeParams)
                {
                    o.RouteParameterDataClasses.Add(param.Key, param.Value);
                }
            }

            var requestHeaders = section.GetSection(nameof(LoggingRedactionOptions.RequestHeadersDataClasses)).Get<Dictionary<string, DataClassification>>();
            if (requestHeaders != null)
            {
                foreach (var param in requestHeaders)
                {
                    o.RequestHeadersDataClasses.Add(param.Key, param.Value);
                }
            }

            var responseHeaders = section.GetSection(nameof(LoggingRedactionOptions.ResponseHeadersDataClasses)).Get<Dictionary<string, DataClassification>>();
            if (responseHeaders != null)
            {
                foreach (var param in responseHeaders)
                {
                    o.ResponseHeadersDataClasses.Add(param.Key, param.Value);
                }
            }
        });
    }

    /// <summary>
    /// Enables enrichment of HTTP logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The original service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
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
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services)
        where T : class, IHttpLogEnricher
    {
        _ = Throw.IfNull(services);
        _ = services.AddHttpLoggingEnrichment();
        return services.AddActivatedSingleton<IHttpLogEnricher, T>();
    }
}
#endif
