// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Self diagnostics class captures the EventSource events sent by OpenTelemetry
/// modules and writes them to local file for internal troubleshooting.
/// </summary>
/// <remarks>
/// This is copied from the OpenTelemetry-dotnet repo
/// https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/src/OpenTelemetry/Internal/SelfDiagnostics.cs
/// as the class is internal and not visible to this project. This will be removed from R9 library
/// in one of the two conditions below.
///  - OpenTelemetry-dotnet will make it internalVisible to R9 library.
///  - This class will be added to OpenTelemetry-dotnet project as public.
/// </remarks>
internal sealed class SelfDiagnostics : IDisposable
{
    internal static readonly TimeProvider TimeProvider = TimeProvider.System;

    /// <summary>
    /// Long-living object that holds relevant resources.
    /// </summary>
    private static readonly SelfDiagnostics _instance = new();
    private readonly SelfDiagnosticsConfigRefresher _configRefresher;

    static SelfDiagnostics()
    {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    internal SelfDiagnostics()
    {
        _configRefresher = new(TimeProvider);
    }

    /// <summary>
    /// No member of SelfDiagnostics class is explicitly called when an EventSource class, say
    /// OpenTelemetryApiEventSource, is invoked to send an event.
    /// To trigger CLR to initialize static fields and static constructors of SelfDiagnostics,
    /// call EnsureInitialized method before any EventSource event is sent.
    /// </summary>
    public static void EnsureInitialized()
    {
        // see the XML comment above.
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    internal static void OnProcessExit(object? sender, EventArgs e) => _instance.Dispose();

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _configRefresher.Dispose();
        }
    }
}
