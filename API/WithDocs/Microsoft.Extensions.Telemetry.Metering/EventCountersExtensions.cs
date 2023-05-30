// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Metering;

/// <summary>
/// Extensions for registering <see cref="T:Microsoft.Extensions.Telemetry.Metering.EventCountersListener" />.
/// </summary>
public static class EventCountersExtensions
{
    /// <summary>
    /// Adds <see cref="T:Microsoft.Extensions.Telemetry.Metering.EventCountersListener" /> to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="configure">An <see cref="T:System.Action" /> to configure the provided <see cref="T:Microsoft.Extensions.Telemetry.Metering.EventCountersCollectorOptions" />.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddEventCounterCollector(this IServiceCollection services, Action<EventCountersCollectorOptions> configure);

    /// <summary>
    /// Adds <see cref="T:Microsoft.Extensions.Telemetry.Metering.EventCountersListener" /> to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="section">An <see cref="T:System.Action" /> to configure the provided <see cref="T:Microsoft.Extensions.Telemetry.Metering.EventCountersCollectorOptions" />.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Either <paramref name="services" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddEventCounterCollector(this IServiceCollection services, IConfigurationSection section);
}
