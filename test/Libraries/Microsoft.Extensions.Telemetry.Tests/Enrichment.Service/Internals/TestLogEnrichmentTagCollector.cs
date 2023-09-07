// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test.Internals;

public class TestLogEnrichmentTagCollector : IEnrichmentTagCollector
{
    private readonly Dictionary<string, object> _tags = new();

    public TestLogEnrichmentTagCollector(IEnumerable<KeyValuePair<string, object>>? input = null)
    {
        if (input != null)
        {
            foreach (var kvp in input)
            {
                _tags.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public IReadOnlyDictionary<string, object> Tags => _tags;

    public void Add(string tagName, object tagValue)
    {
        _tags.Add(tagName, tagValue);
    }
}
