// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Telemetry;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace Microsoft.AspNetCore.Telemetry.Internal;

internal sealed class ConfigureAspNetCoreInstrumentationOptions : IConfigureOptions<AspNetCoreInstrumentationOptions>
{
    private readonly HttpTraceEnrichmentProcessor _enrichmentProcessor;
    private readonly HttpUrlRedactionProcessor _redactionProcessor;

    public ConfigureAspNetCoreInstrumentationOptions(HttpTraceEnrichmentProcessor enrichmentProcessor, HttpUrlRedactionProcessor redactionProcessor)
    {
        _enrichmentProcessor = enrichmentProcessor;
        _redactionProcessor = redactionProcessor;
    }

    public void Configure(AspNetCoreInstrumentationOptions options)
    {
        options.EnrichWithHttpRequest = (activity, request)
            => activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, request);

        options.EnrichWithHttpResponse = (activity, response)
            => EnrichAndRedact(activity, response.HttpContext.Request);

        options.EnrichWithException = (activity, _)
            => EnrichAndRedact(activity, (HttpRequest?)activity.GetCustomProperty(Constants.CustomPropertyHttpRequest));
    }

    private void EnrichAndRedact(Activity activity, HttpRequest? request)
    {
        if (request != null)
        {
            activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, null);

            _enrichmentProcessor.Enrich(activity, request);
            _redactionProcessor.Process(activity, request);
        }
    }
}

#endif
