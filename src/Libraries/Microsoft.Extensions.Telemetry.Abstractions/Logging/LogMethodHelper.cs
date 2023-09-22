// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Utility type to support generated logging methods.
/// </summary>
/// <remarks>
/// This type is not intended to be directly invoked by application code,
/// it is intended to be invoked by generated logging method code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LogMethodHelper : List<KeyValuePair<string, object?>>, ITagCollector, IEnrichmentTagCollector, IResettable
{
    private const string Separator = "_";

    /// <inheritdoc/>
    public void Add(string tagName, object? tagValue)
    {
        _ = Throw.IfNull(tagName);

        string fullName = ParameterName.Length > 0 ? ParameterName + Separator + tagName : tagName;
        Add(new KeyValuePair<string, object?>(fullName, tagValue));
    }

    /// <inheritdoc/>
    public void Add(string tagName, object? tagValue, DataClassification classification) => Add(tagName, tagValue);

    /// <summary>
    /// Resets state of this container as described in <see cref="IResettable.TryReset"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the object successfully reset and can be reused.
    /// </returns>
    public bool TryReset()
    {
        Clear();
        ParameterName = string.Empty;
        return true;
    }

    /// <summary>
    /// Gets or sets the name of the logging method parameter for which to collect tags.
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

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

    private static readonly ObjectPool<LogMethodHelper> _helpers = PoolFactory.CreateResettingPool<LogMethodHelper>();

    /// <summary>
    /// Gets an instance of a helper from the global pool.
    /// </summary>
    /// <returns>A usable instance.</returns>
    public static LogMethodHelper GetHelper() => _helpers.Get();

    /// <summary>
    /// Returns a helper instance to the global pool.
    /// </summary>
    /// <param name="helper">The helper instance.</param>
    public static void ReturnHelper(LogMethodHelper helper) => _helpers.Return(helper);

    /// <inheritdoc/>
    void IEnrichmentTagCollector.Add(string tagName, object tagValue)
    {
        _ = Throw.IfNullOrEmpty(tagName);
        Add(new KeyValuePair<string, object?>(tagName, tagValue));
    }

    /// <summary>
    /// Gets log define options configured to skip the log level enablement check.
    /// </summary>
    public static LogDefineOptions SkipEnabledCheckOptions { get; } = new() { SkipEnabledCheck = true };
}
