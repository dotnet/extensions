// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Telemetry.Http.Logging;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.IO;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods to register the HTTP logging feature within the service.
/// </summary>
public static class HttpLoggingServiceExtensions
{
    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        return AddHttpLoggingInternal(services);
    }

    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="configure">
    /// An <see cref="Action{LoggingOptions}"/> to configure the <see cref="LoggingOptions"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="services"/> or <paramref name="configure"/> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, Action<LoggingOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return AddHttpLoggingInternal(services, x => x.Configure(configure));
    }

    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="section">The configuration section to bind <see cref="LoggingOptions"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
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

        return AddHttpLoggingInternal(services, x => x.Bind(section));
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

        return services.AddActivatedSingleton<IHttpLogEnricher, T>();
    }

    /// <summary>
    /// Registers incoming HTTP request logging middleware into <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Request logging middleware should be placed after <see cref="EndpointRoutingApplicationBuilderExtensions.UseRouting"/> call.
    /// </remarks>
    /// <param name="builder">An application's request pipeline builder.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.UseMiddleware<HttpLoggingMiddleware>(Array.Empty<object>());
    }

    private static IServiceCollection AddHttpLoggingInternal(
        IServiceCollection services,
        Action<OptionsBuilder<LoggingOptions>>? configureOptionsBuilder = null)
    {
        var builder = services
            .AddValidatedOptions<LoggingOptions, LoggingOptionsValidator>();

        configureOptionsBuilder?.Invoke(builder);

        // Register recyclable memory stream manager:
        services.TryAddSingleton<RecyclableMemoryStreamManager>();

        // Register our middleware:
        services.TryAddActivatedSingleton<HttpLoggingMiddleware>();

        // Internal stuff for route processing:
        _ = services.AddHttpRouteProcessor();
        _ = services.AddHttpRouteUtilities();

        return services;
    }
}
