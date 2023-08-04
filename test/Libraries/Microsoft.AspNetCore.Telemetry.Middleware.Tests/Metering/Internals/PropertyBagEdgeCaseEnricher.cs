// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

public class PropertyBagEdgeCaseEnricher : IIncomingRequestMetricEnricher
{
    public IReadOnlyList<string> TagNames => new[] { "non_null_object_property" };
    private readonly object _stringObj = "test_val";

    public void Enrich(IEnrichmentTagCollector collector)
    {
        collector.Add("non_null_object_property", _stringObj);
    }
}
