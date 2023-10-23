// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Shared.Text;
using Polly.Telemetry;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class HttpResilienceMetricsEnricher : MeteringEnricher
{
    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        if (typeof(TResult) != typeof(HttpResponseMessage))
        {
            return;
        }

        if (context.TelemetryEvent.Outcome.HasValue && context.TelemetryEvent.Outcome.Value.Result is HttpResponseMessage response)
        {
            context.Tags.Add(new(HttpResilienceTagNames.FailureReason, ((int)response.StatusCode).ToInvariantString()));
        }
    }
}
