// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class HttpLoggingEnrichmentInterceptor : IHttpLoggingInterceptor
{
    private readonly IHttpLogEnricher[] _enrichers;
    private readonly ILogger<HttpLoggingEnrichmentInterceptor> _logger;

    public HttpLoggingEnrichmentInterceptor(IEnumerable<IHttpLogEnricher> httpLogEnrichers, ILogger<HttpLoggingEnrichmentInterceptor> logger)
    {
        _enrichers = httpLogEnrichers.ToArray();
        if (_enrichers.Length == 0)
        {
            Throw.ArgumentException(nameof(httpLogEnrichers), "No IHttpLogEnricher instances were registered.");
        }

        _logger = logger;
    }

    public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
    {
        // Enrichment only applies to the response.
        return default;
    }

    public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
    {
        // Don't enrich if we're not going to log any part of the response
        // TODO: Make sure HttpLoggingEnrichmentInterceptor runs after HttpLoggingRedactionInterceptor which might
        // exclude some paths from logging.
        if (_enrichers.Length == 0
            || (!logContext.IsAnyEnabled(HttpLoggingFields.Response) && logContext.Parameters.Count == 0))
        {
            return default;
        }

        var context = logContext.HttpContext;
        var loggerMessageState = LoggerMessageHelper.ThreadLocalState;

        try
        {
            foreach (var enricher in _enrichers)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    enricher.Enrich(loggerMessageState, context);
                }
                catch (Exception ex)
                {
                    _logger.EnricherFailed(ex, enricher.GetType().Name);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            foreach (var pair in loggerMessageState)
            {
                logContext.Parameters.Add(pair);
            }
        }
        finally
        {
            loggerMessageState.Clear();
        }

        return default;
    }
}

#endif
