// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETFRAMEWORK
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal class RequestCancellationTestObserver : IObserver<DiagnosticListener>, IDisposable
{
    private const string ListenerName = "HttpHandlerDiagnosticListener";
    private readonly CancellationTokenSource _cts;
    private IDisposable? _observer;
    private IDisposable? _subscription;

    public RequestCancellationTestObserver(CancellationTokenSource cts)
    {
        _cts = cts;
        Initialize();
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == ListenerName)
        {
            _observer?.Dispose();
            _observer = value.SubscribeWithAdapter(this);
        }
    }

    public void OnCompleted()
    {
        // Method intentionally left empty.
    }

    public void OnError(Exception error)
    {
        // Method intentionally left empty.
    }

    public void Dispose()
    {
        _observer?.Dispose();
        _subscription?.Dispose();
    }

    [DiagnosticName("System.Net.Http.HttpRequestOut.Start")]
    public virtual void OnRequestStart(HttpRequestMessage request)
    {
        _cts.Cancel();
    }

    private void Initialize()
    {
        _subscription = DiagnosticListener.AllListeners.Subscribe(this);
    }
}
#endif
