﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal class TestEnricher : IOutgoingRequestMetricEnricher
{
    private readonly List<string> _tagNames = new();
    private readonly int _numTags;
    private readonly string _prefix;

    public TestEnricher()
    {
        _numTags = 1;
        _prefix = string.Empty;

        for (int i = 1; i <= _numTags; i++)
        {
            _tagNames.Add($"test_property_{_prefix}{i}");
        }
    }

    public TestEnricher(int numTags, string prefix = "")
    {
        _numTags = numTags;
        _prefix = prefix;

        for (int i = 1; i <= _numTags; i++)
        {
            _tagNames.Add($"test_property_{_prefix}{i}");
        }
    }

    public IReadOnlyList<string> DimensionNames => _tagNames;

    public void Enrich(IEnrichmentTagCollector collector)
    {
        for (int i = 1; i <= _numTags; i++)
        {
            collector.Add($"test_property_{_prefix}{i}", $"test_value_{_prefix}{i}");
        }
    }
}
