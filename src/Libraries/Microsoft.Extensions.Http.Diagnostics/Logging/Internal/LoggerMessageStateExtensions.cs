// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging.Internal;

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
                static x => HttpClientLoggingTagNames.RequestHeaderPrefix + Normalize(x));

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
                static x => HttpClientLoggingTagNames.ResponseHeaderPrefix + Normalize(x));

            state.TagArray[index++] = new(key, items[i].Value);
        }
    }

    /// <summary>
    /// Adds the full request URL (including the redacted query string) as the <c>url.full</c> tag to the <see cref="LoggerMessageState"/>.
    /// </summary>
    /// <param name="state">The <see cref="LoggerMessageState"/> to be filled.</param>
    /// <param name="fullUri">The full URI to log with redacted query parameters.</param>
    /// <param name="index">
    /// Represents an index to be used when writing tags into <paramref name="state"/>.
    /// <para>This parameter will be mutated to point to the next <paramref name="state"/> item.</para>
    /// </param>
    public static void AddFullUrl(this LoggerMessageState state, string? fullUri, ref int index)
    {
        if (fullUri is not null)
        {
            state.TagArray[index++] = new(HttpClientLoggingTagNames.FullUriPrefix, fullUri);
        }
    }

    /// <summary>
    /// Adds path parameters to <see cref="LoggerMessageState"/>.
    /// </summary>
    /// <param name="state">A <see cref="LoggerMessageState"/> to be filled.</param>
    /// <param name="parameters">An array with path parameters.</param>
    /// <param name="paramsCount">A number of path parameters.</param>
    /// <param name="index">Represents an index to be used when writing tags into <paramref name="state"/>.</param>
    /// <remarks><paramref name="index"/> will be mutated to point to the next <paramref name="state"/> item.</remarks>
    public static void AddPathParameters(this LoggerMessageState state, HttpRouteParameter[] parameters, int paramsCount, ref int index)
    {
        for (var i = 0; i < paramsCount; i++)
        {
            state.TagArray[index++] = new(parameters[i].Name, parameters[i].Value);
        }
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "Normalization to lower case is required by OTel's semantic conventions")]
    private static string Normalize(string header)
        => header.ToLowerInvariant();
}
