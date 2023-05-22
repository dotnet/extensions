// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Console.Internal;
internal readonly struct LogEntryCompositeState
{
    public LogEntryCompositeState(
        IReadOnlyCollection<KeyValuePair<string, object?>>? state,
        ActivityTraceId traceId,
        ActivitySpanId spanId)
    {
        State = state;
        TraceId = traceId;
        SpanId = spanId;
    }

    public IReadOnlyCollection<KeyValuePair<string, object?>>? State { get; }

    public ActivityTraceId TraceId { get; }

    public ActivitySpanId SpanId { get; }
}
#endif
