// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Internals;

internal class PrimitiveValuesEnricher : ILogEnricher
{
    private readonly string? _key;
    private readonly int _value;

    public PrimitiveValuesEnricher(string? key, int value)
    {
        _key = key;
        _value = value;
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        enrichmentBag.Add(_key!, _value!);
    }
}
