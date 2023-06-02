// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETFRAMEWORK
using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Internal;

internal sealed class HttpClientDiagnosticObserver : IObserver<DiagnosticListener>, IDisposable
{
    private const string ListenerName = "HttpHandlerDiagnosticListener";
    private readonly HttpClientRequestAdapter _httpClientRequestAdapter;
    private IDisposable? _observer;

    public HttpClientDiagnosticObserver(HttpClientRequestAdapter httpClientRequestAdapter)
    {
        _httpClientRequestAdapter = httpClientRequestAdapter;
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == ListenerName)
        {
            _observer?.Dispose();
            _observer = value.SubscribeWithAdapter(_httpClientRequestAdapter);
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
    }
}
#endif
