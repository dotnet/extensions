// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal sealed class ManualHealthCheck<T> : IManualHealthCheck<T>
{
    private static readonly object _lock = new();

    private HealthCheckResult _result;

    public HealthCheckResult Result
    {
        get
        {
            lock (_lock)
            {
                return _result;
            }
        }
        set
        {
            lock (_lock)
            {
                _result = value;
            }
        }
    }

    private readonly IManualHealthCheckTracker _tracker;

    [SuppressMessage("Major Code Smell", "S3366:\"this\" should not be exposed from constructors", Justification = "It's OK, just registering into a list")]
    public ManualHealthCheck(IManualHealthCheckTracker tracker)
    {
        Result = HealthCheckResult.Unhealthy("Initial state");

        _tracker = tracker;
        _tracker.Register(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Dispose(bool _)
    {
        _tracker.Unregister(this);
    }

    [ExcludeFromCodeCoverage]
    ~ManualHealthCheck() => Dispose(false);
}
