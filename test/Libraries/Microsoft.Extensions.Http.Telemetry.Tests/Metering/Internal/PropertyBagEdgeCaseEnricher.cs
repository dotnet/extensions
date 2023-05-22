// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal class PropertyBagEdgeCaseEnricher : IOutgoingRequestMetricEnricher
{
    public IReadOnlyList<string> DimensionNames => new[] { "non_null_object_property" };
    private readonly object _stringObj = "test_val";

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        enrichmentBag.Add("non_null_object_property", _stringObj);
    }
}
