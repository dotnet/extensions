// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Telemetry.Metering.Test.Auxiliary;

internal class TestEventSource : EventSource
{
    public EventCommandEventArgs? EventCommandEventArgs { get; set; }

    public TestEventSource(string eventSourceName)
        : base(eventSourceName)
    {
    }

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        EventCommandEventArgs = command;
        base.OnEventCommand(command);
    }
}
