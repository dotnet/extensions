// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.Common'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the telemetry health check publisher.
/// </summary>
[Experimental("EXTEXP0007", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class TelemetryHealthCheckPublisherOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to log only when unhealthy reports are received.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to only log unhealthy reports; <see langword="false" /> to always log.
    /// The default value is <see langword="false" />.
    /// </value>
    public bool LogOnlyUnhealthy { get; set; }

    public TelemetryHealthCheckPublisherOptions();
}
