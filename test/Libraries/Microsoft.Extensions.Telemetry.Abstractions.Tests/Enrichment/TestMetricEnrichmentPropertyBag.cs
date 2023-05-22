// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Enrichment.Test;

public class TestMetricEnrichmentPropertyBag : IEnrichmentPropertyBag
{
    private readonly Dictionary<string, string> _properties = new();

    public TestMetricEnrichmentPropertyBag(IEnumerable<KeyValuePair<string, object>>? input = null)
    {
        if (input != null)
        {
            foreach (var kvp in input)
            {
                _properties.Add(kvp.Key, kvp.Value.ToString() ?? string.Empty);
            }
        }
    }

    public IReadOnlyDictionary<string, string> Properties => _properties;

    public void Add(string key, object value)
    {
        _properties.Add(key, value.ToString() ?? string.Empty);
    }

    public void Add(string key, string value)
    {
        _properties.Add(key, value);
    }

    public void Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
    {
        foreach (var p in properties)
        {
            _properties.Add(p.Key, p.Value.ToString() ?? string.Empty);
        }
    }

    public void Add(ReadOnlySpan<KeyValuePair<string, string>> properties)
    {
        foreach (var p in properties)
        {
            _properties.Add(p.Key, p.Value);
        }
    }
}
