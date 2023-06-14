// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extensions for registering the request latency telemetry middleware.
/// </summary>
public static class RequestLatencyTelemetryExtensions
{
    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>Provided service collection with request latency telemetry middleware added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddScoped(p => p.GetRequiredService<ILatencyContextProvider>().CreateContext());
        services.TryAddSingleton<RequestLatencyTelemetryMiddleware>();

        _ = services.AddValidatedOptions<RequestLatencyTelemetryOptions, RequestLatencyTelemetryOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Configuration of <see cref="RequestLatencyTelemetryOptions"/>.</param>
    /// <returns>Provided service collection with request latency telemetry middleware added.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, Action<RequestLatencyTelemetryOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .Configure(configure);

        return AddRequestLatencyTelemetry(services);
    }

    /// <summary>
    /// Adds request latency telemetry middleware to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="section">Configuration of <see cref="RequestLatencyTelemetryOptions"/>.</param>
    /// <returns>Provided service collection with request latency telemetry middleware added.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, type: typeof(RequestLatencyTelemetryOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddRequestLatencyTelemetry(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services
            .Configure<RequestLatencyTelemetryOptions>(section);

        return AddRequestLatencyTelemetry(services);
    }

    /// <summary>
    /// Adds the request latency telemetry middleware to <see cref="IApplicationBuilder"/> request execution pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.UseMiddleware<RequestLatencyTelemetryMiddleware>(Array.Empty<object>());

        return builder;
    }
}
