// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal class TestEnricher : IOutgoingRequestMetricEnricher
{
    private readonly List<string> _dimensionNames = new();
    private readonly int _numDimensions;
    private readonly string _prefix;

    public TestEnricher()
    {
        _numDimensions = 1;
        _prefix = string.Empty;

        for (int i = 1; i <= _numDimensions; i++)
        {
            _dimensionNames.Add($"test_property_{_prefix}{i}");
        }
    }

    public TestEnricher(int numDimensions, string prefix = "")
    {
        _numDimensions = numDimensions;
        _prefix = prefix;

        for (int i = 1; i <= _numDimensions; i++)
        {
            _dimensionNames.Add($"test_property_{_prefix}{i}");
        }
    }

    public IReadOnlyList<string> DimensionNames => _dimensionNames;

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        for (int i = 1; i <= _numDimensions; i++)
        {
            enrichmentBag.Add($"test_property_{_prefix}{i}", $"test_value_{_prefix}{i}");
        }
    }
}
