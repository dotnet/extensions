// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.HeaderParsing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for header parsing.
/// </summary>
public static class HeaderParsingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services)
    {
        if (!Throw.IfNull(services).Any(x => x.ServiceType == typeof(HeaderParsingFeature.PoolHelper)))
        {
            _ = services
                .AddPooled<HeaderParsingFeature.PoolHelper>()
                .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddSingleton<IHeaderRegistry, HeaderRegistry>()
                .AddScoped(provider => provider.GetRequiredService<ObjectPool<HeaderParsingFeature.PoolHelper>>().Get())
                .AddScoped(provider => provider.GetRequiredService<HeaderParsingFeature.PoolHelper>().Feature)
                .AddSingleton<HeaderParsingMetrics>()
                .AddMetrics();
        }

        return services;
    }

    /// <summary>
    /// Adds the header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">A delegate to setup parsing for the header.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, Action<HeaderParsingOptions> configure)
    {
        _ = Throw.IfNull(services);

        _ = services.AddOptionsWithValidateOnStart<HeaderParsingOptions, HeaderParsingOptionsValidator>();
        _ = services.AddOptionsWithValidateOnStart<HeaderParsingOptions, HeaderParsingOptionsManualValidator>();

        return services
            .AddHeaderParsing()
            .Configure(configure);
    }

    /// <summary>
    /// Adds the header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="section">A configuration section.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);

        _ = services
            .AddOptionsWithValidateOnStart<HeaderParsingOptions, HeaderParsingOptionsValidator>()
            .Bind(section);

        _ = services
            .AddOptionsWithValidateOnStart<HeaderParsingOptions, HeaderParsingOptionsManualValidator>()
            .Bind(section);

        return services
            .AddHeaderParsing();
    }
}
