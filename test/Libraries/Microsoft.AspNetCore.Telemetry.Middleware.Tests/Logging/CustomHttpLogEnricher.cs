// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

internal sealed class CustomHttpLogEnricher : IHttpLogEnricher
{
    internal const string Key1 = "key1";
    internal const string Value1 = "value1";

    internal const string Key2 = "key2";
    internal const double Value2 = 2;

    public void Enrich(IEnrichmentTagCollector collector, HttpRequest request, HttpResponse response)
    {
        collector.Add(Key1, Value1);
        collector.Add(Key2, Value2);
    }
}
