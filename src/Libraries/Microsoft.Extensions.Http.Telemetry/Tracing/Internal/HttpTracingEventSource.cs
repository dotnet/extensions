// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Internal;

[EventSource(Name = "R9-OutgoingHttpTracing-Instrumentation")]
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Event IDs.")]
internal sealed class HttpTracingEventSource : EventSource
{
    internal static readonly HttpTracingEventSource Instance = new();

    private HttpTracingEventSource()
    {
    }

    [Event(2, Level = EventLevel.Error, Message = "Outgoing Http Request URI for Activity (Name = '{activityName}', Id = '{activityId}') was not set.")]
    public void HttpRequestUriWasNotSet(string activityName, string? activityId)
    {
        WriteEvent(2, activityName, activityId);
    }
}
