﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Collections;

namespace Microsoft.AspNetCore.Telemetry;

internal class NullRequestEnricher : IIncomingRequestMetricEnricher
{
    public IReadOnlyList<string> TagNames => Empty.ReadOnlyList<string>();

    public void Enrich(IEnrichmentTagCollector collector)
    {
        collector.Add(null!, null!);
    }
}
