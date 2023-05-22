// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.Http;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Internal;

internal sealed class ConfigureHttpClientInstrumentationOptions : IConfigureOptions<HttpClientInstrumentationOptions>
{
    private readonly HttpClientTraceEnrichmentProcessor _enrichmentProcessor;
    private readonly HttpClientRedactionProcessor _redactionProcessor;

    public ConfigureHttpClientInstrumentationOptions(HttpClientTraceEnrichmentProcessor enrichmentProcessor, HttpClientRedactionProcessor redactionProcessor)
    {
        _enrichmentProcessor = enrichmentProcessor;
        _redactionProcessor = redactionProcessor;
    }

    public void Configure(HttpClientInstrumentationOptions options)
    {
        options.EnrichWithHttpRequestMessage = (activity, request) => activity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, request);
        options.EnrichWithHttpResponseMessage = (activity, response) => EnrichAndRedact(activity, response.RequestMessage, response);
        options.EnrichWithException = (activity, _) => EnrichAndRedact(activity, (HttpRequestMessage?)activity.GetCustomProperty(Constants.CustomPropertyHttpRequestMessage), null);
    }

    private void EnrichAndRedact(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response)
    {
        if (request is not null)
        {
            _enrichmentProcessor.Enrich(activity, request, response);
            _redactionProcessor.Process(activity, request);
        }

        activity.SetCustomProperty(Constants.CustomPropertyHttpRequestMessage, null);
    }
}
