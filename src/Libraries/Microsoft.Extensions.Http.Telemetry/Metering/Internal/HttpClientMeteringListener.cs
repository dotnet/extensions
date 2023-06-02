// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETFRAMEWORK
using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Internal;

internal sealed class HttpClientMeteringListener : IDisposable
{
    internal static bool UsingDiagnosticsSource { get; set; }

    private readonly IDisposable _listener;

    public HttpClientMeteringListener(HttpClientDiagnosticObserver httpClientDiagnosticObserver)
    {
        UsingDiagnosticsSource = true;
        _listener = DiagnosticListener.AllListeners.Subscribe(httpClientDiagnosticObserver);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
#endif
