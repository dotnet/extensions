// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry.Latency.Internal;

internal static class HttpCheckpoints
{
    public const string SocketConnectStart = "cons";
    public const string SocketConnectEnd = "cone";
    public const string ConnectionEstablished = "cones";
    public const string RequestLeftQueue = "rlq";

    public const string NameResolutionStart = "dnss";
    public const string NameResolutionEnd = "dnse";

    public const string RequestHeadersStart = "reqhs";
    public const string RequestHeadersEnd = "reqhe";

    public const string RequestContentStart = "reqcs";
    public const string RequestContentEnd = "reqce";

    public const string ResponseHeadersStart = "reshs";
    public const string ResponseHeadersEnd = "reshe";

    public const string ResponseContentStart = "rescs";
    public const string ResponseContentEnd = "resce";

    public const string HandlerRequestStart = "handreqs";
    public const string EnricherInvoked = "enrin";

    public static readonly string[] Checkpoints = new[]
    {
        SocketConnectStart,
        SocketConnectEnd,
        ConnectionEstablished,
        RequestLeftQueue,

        NameResolutionStart,
        NameResolutionEnd,

        RequestHeadersStart,
        RequestHeadersEnd,

        RequestContentStart,
        RequestContentEnd,

        ResponseHeadersStart,
        ResponseHeadersEnd,

        ResponseContentStart,
        ResponseContentEnd,

        HandlerRequestStart,
        EnricherInvoked
    };
}
