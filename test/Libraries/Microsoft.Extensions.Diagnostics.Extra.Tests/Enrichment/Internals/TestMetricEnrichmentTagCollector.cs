﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Enrichment.Test.Internals;

public class TestMetricEnrichmentTagCollector : IEnrichmentTagCollector
{
    private readonly Dictionary<string, string> _tags = [];

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
}
