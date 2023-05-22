// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Metering.Test.Internal;

internal class TestEnricher : IMetricEnricher
{
    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        enrichmentBag.Add("testKey", "testValue");
    }
}
