// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : IEnrichmentTagCollector
{
    /// <inheritdoc/>
    void IEnrichmentTagCollector.Add(string key, object value)
    {
        AddTag(key, value);
    }

    /// <inheritdoc/>
    void IEnrichmentTagCollector.Add(string key, string value)
    {
        AddTag(key, value);
    }

    /// <inheritdoc/>
    void IEnrichmentTagCollector.Add(ReadOnlySpan<KeyValuePair<string, object>> tags)
    {
        var index = ReserveTagSpace(tags.Length);
        foreach (var p in tags)
        {
            TagArray[index++] = new(p.Key, p.Value);
        }
    }

    /// <inheritdoc/>
    void IEnrichmentTagCollector.Add(ReadOnlySpan<KeyValuePair<string, string>> tags)
    {
        var index = ReserveTagSpace(tags.Length);
        foreach (var p in tags)
        {
            TagArray[index++] = new(p.Key, p.Value);
        }
    }
}
