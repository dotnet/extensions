// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal sealed class HttpLoggingEnrichmentInterceptor : IHttpLoggingInterceptor
{
    private readonly IHttpLogEnricher[] _enrichers;

    public HttpLoggingEnrichmentInterceptor(IEnumerable<IHttpLogEnricher> httpLogEnrichers)
    {
        _enrichers = httpLogEnrichers.ToArray();
    }

    public void OnRequest(HttpLoggingContext logContext)
    {
        // Enrichment only applies to the response.
    }

    public void OnResponse(HttpLoggingContext logContext)
    {
        // Don't enrich if we're not going to log any part of the response
        if (!logContext.IsAnyEnabled(HttpLoggingFields.Response) || _enrichers.Length == 0)
        {
            return;
        }

        var context = logContext.HttpContext;
        var enrichmentBag =  LogMethodHelper.GetHelper();
        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(enrichmentBag, context.Request, context.Response);
        }

        foreach (var pair in enrichmentBag)
        {
            logContext.Parameters.Add(pair);
        }

        LogMethodHelper.ReturnHelper(enrichmentBag);
    }
}

#endif
