// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metering.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Extensions for registering <see cref="EventCountersListener"/>.
/// </summary>
public static class EventCountersExtensions
{
    /// <summary>
    /// Adds <see cref="EventCountersListener"/> to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="configure">An <see cref="Action"/> to configure the provided <see cref="EventCountersCollectorOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddEventCounterCollector(this IServiceCollection services, Action<EventCountersCollectorOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = AddEventCounterCollectorInternal(services)
            .Configure(configure);

        return services;
    }

    /// <summary>
    /// Adds <see cref="EventCountersListener"/> to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="section">An <see cref="Action"/> to configure the provided <see cref="EventCountersCollectorOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(EventCountersCollectorOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IServiceCollection AddEventCounterCollector(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = AddEventCounterCollectorInternal(services)
            .Bind(section);

        return services;
    }

    private static OptionsBuilder<EventCountersCollectorOptions> AddEventCounterCollectorInternal(IServiceCollection services)
    {
        var optionsBuilder = services
            .AddOptionsWithValidateOnStart<EventCountersCollectorOptions, EventCountersCollectorOptionsValidator>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventCountersCollectorOptions>, EventCountersValidator>());
        _ = services.RegisterMetering();
        _ = services.AddActivatedSingleton<EventCountersListener>();

        return optionsBuilder;
    }
}
