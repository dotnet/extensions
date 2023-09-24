// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerServiceCollectionExtensions
{
    /// <summary>
    /// Configure fake logging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogCollectorOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Configure fake logging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, Action<FakeLogCollectorOptions> configure);

    /// <summary>
    /// Configure fake logging with default options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services);
}
