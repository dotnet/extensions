// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods for setting up Request Headers Log Enricher in an <see cref="IServiceCollection" />.
/// </summary>
public static class RequestHeadersEnricherExtensions
{
    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .AddLogEnricherOptions(_ => { })
            .RegisterRequestHeadersEnricher();
    }

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <param name="configure">The <see cref="RequestHeadersLogEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddLogEnricherOptions(configure)
            .RegisterRequestHeadersEnricher();
    }

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="RequestHeadersLogEnricherOptions"/>
    /// in the Request Headers Log Enricher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(RequestHeadersLogEnricherOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .Configure<RequestHeadersLogEnricherOptions>(section)
            .AddLogEnricherOptions(_ => { })
            .RegisterRequestHeadersEnricher();
    }

    private static IServiceCollection RegisterRequestHeadersEnricher(this IServiceCollection services)
    {
        return services
            .AddHttpContextAccessor()
            .AddLogEnricher<RequestHeadersLogEnricher>();
    }

    private static IServiceCollection AddLogEnricherOptions(
        this IServiceCollection services,
        Action<RequestHeadersLogEnricherOptions> configure)
    {
        _ = services
            .AddValidatedOptions<RequestHeadersLogEnricherOptions, RequestHeadersLogEnricherOptionsValidator>()
            .Configure(configure);

        return services;
    }
}
