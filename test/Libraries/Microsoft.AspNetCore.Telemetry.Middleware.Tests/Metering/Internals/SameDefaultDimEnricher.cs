// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry;

public class SameDefaultDimEnricher : IIncomingRequestMetricEnricher
{
    public IReadOnlyList<string> DimensionNames => new[] { "req_host" };

    public void Enrich(IEnrichmentPropertyBag bag)
    {
        bag.Add("req_host", "req_host_value");
    }
}
