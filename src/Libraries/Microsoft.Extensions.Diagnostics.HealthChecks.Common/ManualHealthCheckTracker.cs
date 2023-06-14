// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class ManualHealthCheckTracker
{
    private static readonly HealthCheckResult _healthy = HealthCheckResult.Healthy();

    private readonly ConcurrentDictionary<IManualHealthCheck, bool> _checks = new();

    public void Register(IManualHealthCheck check)
    {
        _ = _checks.AddOrUpdate(check, true, (_, _) => true);
    }

    public void Unregister(IManualHealthCheck checkToRemove)
    {
        _ = _checks.TryRemove(checkToRemove, out _);
    }

    public HealthCheckResult GetHealthCheckResult()
    {
        // Construct string showing all reasons for unhealthy manual health checks
        StringBuilder? stringBuilder = null;

        try
        {
            var worstStatus = HealthStatus.Healthy;
            foreach (var checkPair in _checks)
            {
                var check = checkPair.Key.Result;
                if (check.Status != HealthStatus.Healthy)
                {
                    stringBuilder = (stringBuilder == null) ? PoolFactory.SharedStringBuilderPool.Get() : stringBuilder.Append(", ");
                    _ = stringBuilder.Append(check.Description);
                    if (worstStatus > check.Status)
                    {
                        worstStatus = check.Status;
                    }
                }
            }

            if (stringBuilder == null)
            {
                return _healthy;
            }

            return new HealthCheckResult(worstStatus, stringBuilder.ToString());
        }
        finally
        {
            if (stringBuilder != null)
            {
                PoolFactory.SharedStringBuilderPool.Return(stringBuilder);
            }
        }
    }
}
