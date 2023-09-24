// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register the HTTP logging feature within the service.
/// </summary>
public static class HttpLoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services)
        => Throw.IfNull(services)
        .AddHttpLoggingInternal();

    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="configure">
    /// An <see cref="Action{LoggingOptions}"/> to configure the <see cref="LoggingOptions"/>.
    /// </param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="services"/> or <paramref name="configure"/> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return AddHttpLoggingInternal(services, builder => builder.Configure(configure));
    }

    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="section">The configuration section to bind <see cref="LoggingOptions"/> to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="services"/> or <paramref name="section"/> is <see langword="null" />.
    /// </exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LoggingOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return AddHttpLoggingInternal(services, builder => builder.Bind(section));
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
        => Throw.IfNull(services)
        .AddActivatedSingleton<IHttpLogEnricher, T>();

    private static IServiceCollection AddHttpLoggingInternal(
        this IServiceCollection services,
        Action<OptionsBuilder<LoggingOptions>>? configureOptionsBuilder = null)
    {
        var builder = services
            .AddOptionsWithValidateOnStart<LoggingOptions, LoggingOptionsValidator>();

        configureOptionsBuilder?.Invoke(builder);

        services.TryAddSingleton<RecyclableMemoryStreamManager>();
        services.TryAddActivatedSingleton<HttpLoggingMiddleware>();

        return services
            .AddHttpRouteProcessor()
            .AddHttpRouteUtilities();
    }
}
