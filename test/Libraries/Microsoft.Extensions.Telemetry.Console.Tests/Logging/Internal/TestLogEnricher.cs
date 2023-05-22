// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Console.Internal.Test;

internal sealed class TestLogEnricher : ILogEnricher
{
    public const string Key = "Enriched-Key";
    public const string Value = "Enriched-Value";

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
        => enrichmentBag.Add(Key, Value);
}
