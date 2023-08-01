// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : IEnrichmentPropertyBag
{
    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(string key, object value)
    {
        AddProperty(key, value);
    }

    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(string key, string value)
    {
        AddProperty(key, value);
    }

    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
    {
        foreach (var p in properties)
        {
            AddProperty(p.Key, p.Value);
        }
    }

    /// <inheritdoc/>
    void IEnrichmentPropertyBag.Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
    {
        foreach (var p in properties)
        {
            AddProperty(p.Key, p.Value);
        }
    }
}
