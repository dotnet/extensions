// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Telemetry.Internal;

[EventSource(Name = "R9-SelfDiagnostics")]
internal sealed class SelfDiagnosticsEventSource : EventSource
{
    public const int FileCreateExceptionEventId = 26;

    public static readonly SelfDiagnosticsEventSource Log = new();

    [Event(FileCreateExceptionEventId, Message = "Failed to create file. LogDirectory ='{0}', Id = '{1}'.", Level = EventLevel.Warning)]
    public void SelfDiagnosticsFileCreateException(string logDirectory, string exception)
    {
        WriteEvent(FileCreateExceptionEventId, logDirectory, exception);
    }

    [NonEvent]
    public void SelfDiagnosticsFileCreateException(string logDirectory, Exception ex)
    {
        if (IsEnabled(EventLevel.Warning, EventKeywords.All))
        {
            SelfDiagnosticsFileCreateException(logDirectory, ex.ToString());
        }
    }
}

