// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.Common'

using System;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Lets you manually set the application's health status.
/// </summary>
/// <typeparam name="T">The type of <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.IManualHealthCheck" />.</typeparam>
public interface IManualHealthCheck<T> : IManualHealthCheck, IDisposable
{
}
