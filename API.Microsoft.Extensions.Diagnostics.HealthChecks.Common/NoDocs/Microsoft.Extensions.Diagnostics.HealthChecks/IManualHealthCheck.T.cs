// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.Common'

using System;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public interface IManualHealthCheck<T> : IManualHealthCheck, IDisposable
{
}
