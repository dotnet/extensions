// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for configuring logging enrichment features.
/// </summary>
public static class LoggingEnrichmentExtensions
{
    /// <summary>
    /// Enables enrichment functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder);

    /// <summary>
    /// Enables enrichment functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="configure">Delegate the fine-tune the options.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, Action<LoggerEnrichmentOptions> configure);

    /// <summary>
    /// Enables enrichment functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.Extensions.Logging.LoggerEnrichmentOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static ILoggingBuilder EnableEnrichment(this ILoggingBuilder builder, IConfigurationSection section);
}
