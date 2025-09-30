// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Latency.Internal;

internal static class HttpMeasures
{
    public const string GCPauseTime = "gcp";
    public const string ConnectionInitiated = "coni";

    public static readonly string[] Measures =
    [
        GCPauseTime,
        ConnectionInitiated
    ];
}
