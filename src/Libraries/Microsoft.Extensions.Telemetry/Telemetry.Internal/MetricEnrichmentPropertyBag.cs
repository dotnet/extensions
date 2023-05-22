// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Canonical implementation of a metric enrichment property bag.
/// </summary>
internal sealed class MetricEnrichmentPropertyBag : List<KeyValuePair<string, string>>, IEnrichmentPropertyBag, IResettable
{
    /// <inheritdoc/>
    public void Add(string key, object value)
    {
        _ = Throw.IfNullOrEmpty(key);
        _ = Throw.IfNull(value);

        Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty));
    }

    /// <inheritdoc/>
    public void Add(string key, string value)
    {
        _ = Throw.IfNullOrEmpty(key);
        _ = Throw.IfNull(value);

        Add(new KeyValuePair<string, string>(key, value));
    }

    /// <inheritdoc/>
    public void Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
    {
        foreach (var p in properties)
        {
            Add(p);
        }
    }

    /// <inheritdoc/>
    public void Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
    {
        foreach (var p in properties)
        {
            Add(new KeyValuePair<string, string>(p.Key, p.Value.ToString() ?? string.Empty));
        }
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        Clear();
        return true;
    }
}
