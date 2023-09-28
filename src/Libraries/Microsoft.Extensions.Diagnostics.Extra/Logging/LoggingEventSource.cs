// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Logging;

[EventSource(Name = "Logging-Instrumentation")]
internal sealed class LoggingEventSource : EventSource
{
    public static readonly LoggingEventSource Instance = new();

    [NonEvent]
    internal void LoggingException(Exception ex)
    {
        LoggingException(ex.ToString());
    }

    [Event(1, Message = "An exception occurred while logging: {0}.", Level = EventLevel.Error)]
    private void LoggingException(string exception)
    {
        WriteEvent(1, exception);
    }
}
