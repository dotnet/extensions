// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test.Internals;

public class TestMetricEnrichmentTagCollector : IEnrichmentTagCollector
{
    private readonly Dictionary<string, string> _tags = new();

    public TestMetricEnrichmentTagCollector(IEnumerable<KeyValuePair<string, object>>? input = null)
    {
        if (input != null)
        {
            foreach (var kvp in input)
            {
                _tags.Add(kvp.Key, kvp.Value.ToString() ?? string.Empty);
            }
        }
    }

    public IReadOnlyDictionary<string, string> Tags => _tags;

    public void Add(string tagName, object tagValue)
    {
        _tags.Add(tagName, tagValue.ToString() ?? string.Empty);
    }

    public void Add(string tagName, string tagValue)
    {
        _tags.Add(tagName, tagValue);
    }

    public void Add(ReadOnlySpan<KeyValuePair<string, object>> tags)
    {
        foreach (var p in tags)
        {
            _tags.Add(p.Key, p.Value.ToString() ?? string.Empty);
        }
    }

    public void Add(ReadOnlySpan<KeyValuePair<string, string>> tags)
    {
        foreach (var p in tags)
        {
            _tags.Add(p.Key, p.Value);
        }
    }
}
