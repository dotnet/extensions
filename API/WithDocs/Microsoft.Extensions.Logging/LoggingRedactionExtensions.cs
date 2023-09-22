// Assembly 'Microsoft.Extensions.Telemetry'

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for configuring logging redaction features.
/// </summary>
public static class LoggingRedactionExtensions
{
    /// <summary>
    /// Enables redaction functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static ILoggingBuilder EnableRedaction(this ILoggingBuilder builder);
}
