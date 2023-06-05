// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Common.Tests;

internal class MockHostApplicationLifetime : IHostApplicationLifetime, IDisposable
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly CancellationTokenSource _stopped = new();

    public CancellationToken ApplicationStarted { get; }

    public CancellationToken ApplicationStopping { get; }

    public CancellationToken ApplicationStopped { get; }

    public MockHostApplicationLifetime()
    {
        ApplicationStarted = _started.Token;
        ApplicationStopping = _stopping.Token;
        ApplicationStopped = _stopped.Token;
    }

    public void StartApplication()
    {
        _started.Cancel();
    }

    public void StoppingApplication()
    {
        _stopping.Cancel();
    }

    public void StopApplication()
    {
        _stopped.Cancel();
    }

    public void Dispose()
    {
        _started.Dispose();
        _stopping.Dispose();
        _stopped.Dispose();
    }
}
