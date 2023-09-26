// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Http.Diagnostics.Test;

internal sealed class TestEventListener : EventListener
{
    public TestEventListener(EventSource eventSource, EventLevel eventLevel)
    {
        EnableEvents(eventSource, eventLevel);
    }

    public EventWrittenEventArgs? LastEvent { get; private set; }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        LastEvent = eventData;
    }
}

