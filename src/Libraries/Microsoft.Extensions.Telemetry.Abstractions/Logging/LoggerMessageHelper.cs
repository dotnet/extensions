// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Utility type to support generated logging methods.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
public static class LoggerMessageHelper
{
    [ThreadStatic]
    private static LoggerMessageState? _properties;

    /// <summary>
    /// Gets a thread-local instance of this type.
    /// </summary>
    public static LoggerMessageState ThreadLocalState
    {
        get
        {
            var result = _properties;
            if (result == null)
            {
                result = new();
                _properties = result;
            }

            return result;
        }
    }

    /// <summary>
    /// Enumerates an enumerable into a string.
    /// </summary>
    /// <param name="enumerable">The enumerable object.</param>
    /// <returns>
    /// A string representation of the enumerable.
    /// </returns>
    public static string Stringify(IEnumerable? enumerable)
    {
        if (enumerable == null)
        {
            return "null";
        }

        var sb = PoolFactory.SharedStringBuilderPool.Get();
        _ = sb.Append('[');

        bool first = true;
        foreach (object? e in enumerable)
        {
            if (!first)
            {
                _ = sb.Append(',');
            }

            if (e == null)
            {
                _ = sb.Append("null");
            }
            else
            {
                _ = sb.Append(FormattableString.Invariant($"\"{e}\""));
            }

            first = false;
        }

        _ = sb.Append(']');
        var result = sb.ToString();
        PoolFactory.SharedStringBuilderPool.Return(sb);
        return result;
    }

    /// <summary>
    /// Enumerates an enumerable of key/value pairs into a string.
    /// </summary>
    /// <typeparam name="TKey">Type of keys.</typeparam>
    /// <typeparam name="TValue">Type of values.</typeparam>
    /// <param name="enumerable">The enumerable object.</param>
    /// <returns>
    /// A string representation of the enumerable.
    /// </returns>
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable)
    {
        if (enumerable == null)
        {
            return "null";
        }

        var sb = PoolFactory.SharedStringBuilderPool.Get();
        _ = sb.Append('{');

        bool first = true;
        foreach (var kvp in enumerable)
        {
            if (!first)
            {
                _ = sb.Append(',');
            }

            if (typeof(TKey).IsValueType || kvp.Key is not null)
            {
                _ = sb.Append(FormattableString.Invariant($"\"{kvp.Key}\"="));
            }
            else
            {
                _ = sb.Append("null=");
            }

            if (typeof(TValue).IsValueType || kvp.Value is not null)
            {
                _ = sb.Append(FormattableString.Invariant($"\"{kvp.Value}\""));
            }
            else
            {
                _ = sb.Append("null");
            }

            first = false;
        }

        _ = sb.Append('}');
        var result = sb.ToString();
        PoolFactory.SharedStringBuilderPool.Return(sb);
        return result;
    }
}
