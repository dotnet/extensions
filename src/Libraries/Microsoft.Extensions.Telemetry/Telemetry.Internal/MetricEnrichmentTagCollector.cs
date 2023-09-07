// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Canonical implementation of a metric enrichment tag collector.
/// </summary>
internal sealed class MetricEnrichmentTagCollector : List<KeyValuePair<string, string>>, IEnrichmentTagCollector, IResettable
{
    /// <inheritdoc/>
    public void Add(string key, object value)
    {
        _ = Throw.IfNullOrEmpty(key);
        _ = Throw.IfNull(value);

        Add(new KeyValuePair<string, string>(key, value.ToString() ?? string.Empty));
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        Clear();
        return true;
    }
}
