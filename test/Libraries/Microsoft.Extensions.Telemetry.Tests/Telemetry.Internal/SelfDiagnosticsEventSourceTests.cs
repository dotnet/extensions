// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

public class SelfDiagnosticsEventSourceTests
{
    [Fact(Skip = "Flaky")]
    public void EventSource_WritesEvent()
    {
        using var listener = new TestEventListener(SelfDiagnosticsEventSource.Log, EventLevel.Warning);

        SelfDiagnosticsEventSource.Log.SelfDiagnosticsFileCreateException("test", new ArgumentException("test as well."));

        var lastEvent = listener.LastEvent;

        Assert.NotNull(lastEvent);
        Assert.Equal(SelfDiagnosticsEventSource.FileCreateExceptionEventId, lastEvent!.EventId);
        Assert.Equal(EventLevel.Warning, lastEvent!.Level);
        Assert.Contains("test", lastEvent!.Payload!);
    }
}
