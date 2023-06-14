#pragma warning disable IDE0073

// <copyright file="SelfDiagnostics.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

// This code was originally copied from the OpenTelemetry-dotnet repo
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/src/OpenTelemetry/Internal/SelfDiagnostics.cs

using System;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Self diagnostics class captures the EventSource events sent by OpenTelemetry
/// modules and writes them to local file for internal troubleshooting.
/// </summary>
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
