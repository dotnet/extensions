// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Internals;

internal class EmptyEnricher : ILogEnricher
{
    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        // intentionally left empty
    }
}
