// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal class NullOutgoingRequestMetricEnricher : IOutgoingRequestMetricEnricher
{
    public IReadOnlyList<string> DimensionNames => null!;

    public void Enrich(IEnrichmentTagCollector collector)
    {
        collector.Add(null!, null!);
    }
}
