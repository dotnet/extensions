// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Enriches logs with Request headers information.
/// </summary>
internal sealed class RequestHeadersLogEnricher : ILogEnricher
{
    private readonly FrozenDictionary<string, DataClassification> _headersDataClasses;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRedactorProvider? _redactorProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestHeadersLogEnricher"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">HttpContextAccessor responsible for obtaining properties from HTTP context.</param>
    /// <param name="options">Options to customize configuration of <see cref="RequestHeadersLogEnricher"/>.</param>
    /// <param name="redactorProvider">RedactorProvider to get redactor to redact enriched data according to the data class.</param>
    public RequestHeadersLogEnricher(IHttpContextAccessor httpContextAccessor, IOptions<RequestHeadersLogEnricherOptions> options,
        IRedactorProvider? redactorProvider = null)
    {
        var opt = Throw.IfMemberNull(options, options.Value);
        _httpContextAccessor = httpContextAccessor;

        _headersDataClasses = opt.HeadersDataClasses.Count == 0
            ? FrozenDictionary<string, DataClassification>.Empty
            : opt.HeadersDataClasses.ToFrozenDictionary(StringComparer.Ordinal, optimizeForReading: true);

        if (_headersDataClasses.Count > 0)
        {
            _redactorProvider = Throw.IfNull(redactorProvider);
        }
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentBag)
    {
        if (_httpContextAccessor.HttpContext?.Request == null)
        {
            return;
        }

        var request = _httpContextAccessor.HttpContext.Request;

        if (_headersDataClasses.Count == 0)
        {
            return;
        }

        if (_headersDataClasses.Count != 0)
        {
            foreach (var header in _headersDataClasses)
            {
                if (request.Headers.TryGetValue(header.Key, out var headerValue) && !string.IsNullOrEmpty(headerValue))
                {
                    var redactor = _redactorProvider!.GetRedactor(header.Value);
                    var redacted = redactor.Redact(headerValue);
                    enrichmentBag.Add(header.Key, redacted);
                }
            }
        }
    }
}
