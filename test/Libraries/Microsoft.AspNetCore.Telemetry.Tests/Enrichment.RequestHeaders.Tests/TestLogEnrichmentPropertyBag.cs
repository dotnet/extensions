// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry.RequestHeaders.Test;

public class TestLogEnrichmentPropertyBag : IEnrichmentPropertyBag
{
    private readonly Dictionary<string, object> _properties = new();

    public TestLogEnrichmentPropertyBag(IEnumerable<KeyValuePair<string, object>>? input = null)
    {
        if (input != null)
        {
            foreach (var kvp in input)
            {
                _properties.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public IReadOnlyDictionary<string, object> Properties => _properties;

    public void Add(string key, object value)
    {
        _properties.Add(key, value);
    }

    public void Add(string key, string value)
    {
        _properties.Add(key, value);
    }

    public void Add(ReadOnlySpan<KeyValuePair<string, object>> properties)
    {
        foreach (var p in properties)
        {
            _properties.Add(p.Key, p.Value);
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
