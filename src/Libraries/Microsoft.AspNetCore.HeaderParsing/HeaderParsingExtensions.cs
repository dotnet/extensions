// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Extensions exposing HeaderParsing feature.
/// </summary>
public static class HeaderParsingExtensions
{
    /// <summary>
    /// Adds header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
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
                .RegisterMetering();
        }

        return services;
    }

    /// <summary>
    /// Adds header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">A delegate to setup parsing for the header.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, Action<HeaderParsingOptions> configuration)
    {
        _ = Throw.IfNull(services);

        _ = services.AddOptionsWithValidateOnStart<HeaderParsingOptions, HeaderParsingOptionsValidator>();
        _ = services.AddOptionsWithValidateOnStart<HeaderParsingOptions, HeaderParsingOptionsManualValidator>();

        return services
            .AddHeaderParsing()
            .Configure(configuration);
    }

    /// <summary>
    /// Adds header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="section">A configuration section.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
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

    /// <summary>
    /// Gets the header parsing feature to access parsed header values.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
    /// <returns>The <see cref="HeaderParsingFeature"/> to access parsed header values.</returns>
    public static HeaderParsingFeature GetHeaderParsing(this HttpRequest request)
    {
        var context = Throw.IfNull(request).HttpContext;

        var feature = context.Features.Get<HeaderParsingFeature>();

        if (feature is null)
        {
            feature = context.RequestServices.GetRequiredService<HeaderParsingFeature>();
            feature.Context = context;
            context.Features.Set(feature);
        }

        return feature;
    }

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <returns><see langword="true"/> if the header value was successfully fetched parsed.</returns>
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value)
        where T : notnull
    {
        return Throw.IfNull(request)
            .GetHeaderParsing()
            .TryGetHeaderValue(Throw.IfNull(header), out value);
    }

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="request">The <see cref="HttpRequest"/> instance.</param>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <param name="result">Details on the parsing operation.</param>
    /// <returns><see langword="true"/> if the header value was successfully fetched parsed.</returns>
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result)
        where T : notnull
    {
        return Throw.IfNull(request)
            .GetHeaderParsing()
            .TryGetHeaderValue(Throw.IfNull(header), out value, out result);
    }
}
