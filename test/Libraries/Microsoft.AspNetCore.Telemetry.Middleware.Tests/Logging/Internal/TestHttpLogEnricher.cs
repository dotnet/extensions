// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

internal sealed class TestHttpLogEnricher : IHttpLogEnricher
{
    internal const string Key1 = "MyEnrichedProperty_1";
    internal const string Value1 = "my_value";

    internal const string Key2 = "MyEnrichedProperty_2";
    internal const double Value2 = 1.75;

    public void Enrich(IEnrichmentPropertyBag bag, HttpRequest request, HttpResponse response)
    {
        bag.Add(Key1, Value1);
        bag.Add(Key2, Value2);
    }
}
