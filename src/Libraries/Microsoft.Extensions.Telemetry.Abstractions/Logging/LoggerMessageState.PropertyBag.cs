// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : IEnrichmentPropertyBag
{
    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(string key, object value)
    {
        var s = AllocPropertySpace(1);
        s[0] = new(key, value);
    }

    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(string key, string value)
    {
        var s = AllocPropertySpace(1);
        s[0] = new(key, value);
    }

    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
    {
        var s = AllocPropertySpace(properties.Length);

        int i = 0;
        foreach (var p in properties)
        {
            s[i++] = new(p.Key, p.Value);
        }
    }

    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
    {
        var s = AllocPropertySpace(properties.Length);

        int i = 0;
        foreach (var p in properties)
        {
            s[i++] = new(p.Key, p.Value);
        }
    }
}
