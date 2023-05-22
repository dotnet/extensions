// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
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

        var optionsBuilder = AddEventCounterCollectorInternal(services);

#if NET7_0_OR_GREATER
        _ = optionsBuilder.Bind(section);
#else
        // Regular call:
        // optionsBuilder.Bind(section)

        // Translates to:
        // optionsBuilder.Services.Configure<EventCountersCollectorOptions>(optionsBuilder.Name, section)

        // Above call to Configure<T>() contains following:
        // services.AddSingleton<IOptionsChangeTokenSource<EventCountersCollectorOptions>>(
        //    new ConfigurationChangeTokenSource<EventCountersCollectorOptions>(optionsBuilder.Name, section))

        // services.AddSingleton<IConfigureOptions<EventCountersCollectorOptions>>(
        //    new NamedConfigureFromConfigurationOptions<EventCountersCollectorOptions>(optionsBuilder.Name, section))

        // Since NamedConfigureFromConfigurationOptions<T> calls ConfigurationBinder.Bind(),
        // we need to use our custom version that calls custom binder with added BindToSet() method:
        _ = services.AddSingleton<IOptionsChangeTokenSource<EventCountersCollectorOptions>>(
            new ConfigurationChangeTokenSource<EventCountersCollectorOptions>(optionsBuilder.Name, section));

        _ = services.AddSingleton<IConfigureOptions<EventCountersCollectorOptions>>(
            new CustomConfigureNamedOptions(optionsBuilder.Name, section));
#endif

        return services;
    }

    private static OptionsBuilder<EventCountersCollectorOptions> AddEventCounterCollectorInternal(IServiceCollection services)
    {
        var optionsBuilder = services
            .AddValidatedOptions<EventCountersCollectorOptions, EventCountersCollectorOptionsValidator>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<EventCountersCollectorOptions>, EventCountersValidator>());
        _ = services.RegisterMetering();
        _ = services.AddActivatedSingleton<EventCountersListener>();

        return optionsBuilder;
    }
}
