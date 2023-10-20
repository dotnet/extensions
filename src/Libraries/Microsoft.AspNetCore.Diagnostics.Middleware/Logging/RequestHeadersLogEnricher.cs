// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Enriches logs with Request headers information.
/// </summary>
internal sealed class RequestHeadersLogEnricher : ILogEnricher
{
    private readonly KeyValuePair<string, DataClassification>[] _headersDataClasses;
    private readonly string[] _normalizedHeaders;
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

        _headersDataClasses = opt.HeadersDataClasses.Count == 0 ? [] : opt.HeadersDataClasses.ToArray();
        _normalizedHeaders = HeaderNormalizer.PrepareNormalizedHeaderNames(_headersDataClasses, HttpLoggingTagNames.RequestHeaderPrefix);

        if (_headersDataClasses.Length > 0)
        {
            _redactorProvider = Throw.IfNull(redactorProvider);
        }
    }

    public void Enrich(IEnrichmentTagCollector collector)
    {
        try
        {
            if (_httpContextAccessor.HttpContext?.Request == null)
            {
                return;
            }

            var request = _httpContextAccessor.HttpContext.Request;

            if (_headersDataClasses.Length == 0)
            {
                return;
            }

            if (_headersDataClasses.Length != 0)
            {
                for (int i = 0; i < _headersDataClasses.Length; i++)
                {
                    var header = _headersDataClasses[i];

                    if (request.Headers.TryGetValue(header.Key, out var headerValue) && !string.IsNullOrEmpty(headerValue))
                    {
                        var redactor = _redactorProvider!.GetRedactor(header.Value);
                        var redacted = redactor.Redact(headerValue);
                        collector.Add(_normalizedHeaders[i], redacted);
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Noop.
        }
    }
}
