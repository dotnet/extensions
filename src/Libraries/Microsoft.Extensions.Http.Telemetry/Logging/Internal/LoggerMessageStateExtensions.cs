// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal static class LoggerMessageStateExtensions
{
    private static readonly ConcurrentDictionary<string, string> _requestPrefixedNamesCache = new();
    private static readonly ConcurrentDictionary<string, string> _responsePrefixedNamesCache = new();

    /// <summary>
    /// Adds request headers to <see cref="LoggerMessageState"/>.
    /// </summary>
    /// <param name="state">A <see cref="LoggerMessageState"/> to be filled.</param>
    /// <param name="items">A list with request headers.</param>
    /// <param name="index">Represents an index to be used when writing tags into <paramref name="state"/>.</param>
    /// <remarks><paramref name="index"/> will be mutated to point to the next <paramref name="state"/> item.</remarks>
    public static void AddRequestHeaders(this LoggerMessageState state, List<KeyValuePair<string, string>> items, ref int index)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var key = _requestPrefixedNamesCache.GetOrAdd(
                items[i].Key,
                static (x, p) => p + x,
                HttpClientLoggingTagNames.RequestHeaderPrefix);

            state.TagArray[index++] = new(key, items[i].Value);
        }
    }

    /// <summary>
    /// Adds response headers to <see cref="LoggerMessageState"/>.
    /// </summary>
    /// <param name="state">A <see cref="LoggerMessageState"/> to be filled.</param>
    /// <param name="items">A list with response headers.</param>
    /// <param name="index">Represents an index to be used when writing tags into <paramref name="state"/>.</param>
    /// <remarks><paramref name="index"/> will be mutated to point to the next <paramref name="state"/> item.</remarks>
    public static void AddResponseHeaders(this LoggerMessageState state, List<KeyValuePair<string, string>> items, ref int index)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var key = _responsePrefixedNamesCache.GetOrAdd(
                items[i].Key,
                static (x, p) => p + x,
                HttpClientLoggingTagNames.ResponseHeaderPrefix);

            state.TagArray[index++] = new(key, items[i].Value);
        }
    }
}
