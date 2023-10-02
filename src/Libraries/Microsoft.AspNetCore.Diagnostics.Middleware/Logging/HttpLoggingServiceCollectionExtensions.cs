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
public static class HttpLoggingServiceCollectionExtensions
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
    /// Enables enrichment redaction of HTTP logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="section">The configuration section with the redaction settings.</param>
    /// <returns>The original service collection.</returns>
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(section);

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

            var routeParams = section.GetSection(nameof(LoggingRedactionOptions.RouteParameterDataClasses));
            foreach (var entry in routeParams.GetChildren())
            {
                var taxonomy = entry.GetValue<string>(nameof(DataClassification.TaxonomyName));
                var value = entry.GetValue<ulong>(nameof(DataClassification.Value));
                if (taxonomy != null)
                {
                    o.RouteParameterDataClasses.Add(entry.Key, new DataClassification(taxonomy, value));
                }
            }

            var requestHeaders = section.GetSection(nameof(LoggingRedactionOptions.RequestHeadersDataClasses));
            foreach (var entry in requestHeaders.GetChildren())
            {
                var taxonomy = entry.GetValue<string>(nameof(DataClassification.TaxonomyName));
                var value = entry.GetValue<ulong>(nameof(DataClassification.Value));
                if (taxonomy != null)
                {
                    o.RequestHeadersDataClasses.Add(entry.Key, new DataClassification(taxonomy, value));
                }
            }

            var responseHeaders = section.GetSection(nameof(LoggingRedactionOptions.ResponseHeadersDataClasses));
            foreach (var entry in responseHeaders.GetChildren())
            {
                var taxonomy = entry.GetValue<string>(nameof(DataClassification.TaxonomyName));
                var value = entry.GetValue<ulong>(nameof(DataClassification.Value));
                if (taxonomy != null)
                {
                    o.ResponseHeadersDataClasses.Add(entry.Key, new DataClassification(taxonomy, value));
                }
            }
        });
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
        _ = services.AddHttpLoggingRedaction();
        return services.AddActivatedSingleton<IHttpLogEnricher, T>();
    }
}
#endif
