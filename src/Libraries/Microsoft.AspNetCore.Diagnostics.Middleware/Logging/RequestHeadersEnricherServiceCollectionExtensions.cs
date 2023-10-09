// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Request Headers Log Enricher in an <see cref="IServiceCollection" />.
/// </summary>
public static class RequestHeadersEnricherServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        _ = services.AddOptionsWithValidateOnStart<RequestHeadersLogEnricherOptions, RequestHeadersLogEnricherOptionsValidator>();
        return services
            .AddHttpContextAccessor()
            .AddLogEnricher<RequestHeadersLogEnricher>();
    }

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Request Headers Log Enricher to.</param>
    /// <param name="configure">The <see cref="RequestHeadersLogEnricherOptions"/> configuration delegate.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .Configure(configure)
            .AddRequestHeadersLogEnricher();
    }
}
