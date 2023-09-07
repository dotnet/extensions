// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.Common'

using System;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Lets you manually set the health status of the application.
/// </summary>
public interface IManualHealthCheck : IDisposable
{
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    HealthCheckResult Result { get; set; }
}
