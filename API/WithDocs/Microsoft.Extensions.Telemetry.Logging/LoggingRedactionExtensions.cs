// Assembly 'Microsoft.Extensions.Telemetry'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Extensions for configuring logging redaction features.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class LoggingRedactionExtensions
{
    /// <summary>
    /// Enables redaction functionality within the logging infrastructure.
    /// </summary>
    /// <param name="builder">The dependency injection container to add logging to.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static ILoggingBuilder EnableRedaction(this ILoggingBuilder builder);
}
