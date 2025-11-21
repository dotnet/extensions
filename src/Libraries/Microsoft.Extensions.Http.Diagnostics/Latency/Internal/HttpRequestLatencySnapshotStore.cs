// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Latency.Internal;

/// <summary>
/// Internal helper to persist an immutable latency snapshot on the request.
/// </summary>
internal static class HttpRequestLatencySnapshotStore
{
#if NET6_0_OR_GREATER
    private static readonly HttpRequestOptionsKey<LatencySnapshot> _key = new("LatencySnapshot");
    public static void Set(HttpRequestMessage request, LatencySnapshot snapshot)
        => request.Options.Set(_key, snapshot);

    public static bool TryGet(HttpRequestMessage request, out LatencySnapshot? snapshot)
        => request.Options.TryGetValue(_key, out snapshot);
#else
    private const string Key = "LatencySnapshot";
    public static void Set(HttpRequestMessage request, LatencySnapshot snapshot)
        => request.Properties[Key] = snapshot;

    public static bool TryGet(HttpRequestMessage request, out LatencySnapshot? snapshot)
    {
        if (request.Properties.TryGetValue(Key, out var obj) && obj is LatencySnapshot s)
        {
            snapshot = s;
            return true;
        }

        snapshot = null;
        return false;
    }
#endif
}
