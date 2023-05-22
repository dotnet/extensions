// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// EventSource implementation for R9 logging implementation.
/// </summary>
[EventSource(Name = "R9-Logging-Instrumentation")]
internal sealed class LoggingEventSource : EventSource
{
    public static readonly LoggingEventSource Log = new();

    [NonEvent]
    internal void LogException(Exception ex)
    {
        LogException(ex.ToString());
    }

    [Event(1, Message = "Exception occurred during logging. {exception}.", Level = EventLevel.Error)]
    private void LogException(string exception)
    {
        WriteEvent(1, exception);
    }
}
