// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

[EventSource(Name = "DotNet-SelfDiagnostics-Test")]
internal sealed class TestEventSource : EventSource
{
    public const string WarningMessageText = "This is a warning event.";
    public const string ErrorMessageText = "This is an error event.";
    public const string CriticalMessageText = "This is a critical event.";
    public const string VerboseMessageText = "This is a verbose event.";

    public static readonly TestEventSource Log = new();

    [Event(1, Message = WarningMessageText, Level = EventLevel.Warning)]
    public void WarningEvent()
    {
        if (IsEnabled(EventLevel.Warning, EventKeywords.All))
        {
            WriteEvent(1);
        }
    }

    [Event(2, Message = ErrorMessageText, Level = EventLevel.Error)]
    public void ErrorEvent()
    {
        if (IsEnabled(EventLevel.Error, EventKeywords.All))
        {
            WriteEvent(2);
        }
    }

    [Event(3, Message = CriticalMessageText, Level = EventLevel.Critical)]
    public void CriticalEvent(string message)
    {
        if (IsEnabled(EventLevel.Critical, EventKeywords.All))
        {
            WriteEvent(3, message);
        }
    }

    [Event(4, Message = VerboseMessageText, Level = EventLevel.Verbose)]
    public void VerboseEvent()
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
        {
            WriteEvent(4);
        }
    }
}

