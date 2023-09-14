// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging.Testing;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerExtensions
{
    /// <summary>
    /// Configure fake logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogCollectorOptions" />.</param>
    /// <returns>Logging <paramref name="builder" />.</returns>
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Configure fake logging.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>Logging <paramref name="builder" />.</returns>
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, Action<FakeLogCollectorOptions> configure);

    /// <summary>
    /// Configure fake logging with default options.
    /// </summary>
    /// <param name="builder">Logging builder.</param>
    /// <returns>Logging <paramref name="builder" />.</returns>
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder);

    /// <summary>
    /// Configure fake logging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.Extensions.Logging.Testing.FakeLogCollectorOptions" />.</param>
    /// <returns>Service collection for API chaining.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Configure fake logging.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Logging configuration options.</param>
    /// <returns>Service collection for API chaining.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, Action<FakeLogCollectorOptions> configure);

    /// <summary>
    /// Configure fake logging with default options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for API chaining.</returns>
    public static IServiceCollection AddFakeLogging(this IServiceCollection services);

    /// <summary>
    /// Gets the object that collects log records sent to the fake logger.
    /// </summary>
    /// <param name="services">The service provider containing the logger.</param>
    /// <exception cref="T:System.InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector which tracks records logged to fake loggers.</returns>
    public static FakeLogCollector GetFakeLogCollector(this IServiceProvider services);
}
