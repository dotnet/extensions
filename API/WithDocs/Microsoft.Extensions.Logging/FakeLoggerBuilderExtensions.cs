// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerBuilderExtensions
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
}
