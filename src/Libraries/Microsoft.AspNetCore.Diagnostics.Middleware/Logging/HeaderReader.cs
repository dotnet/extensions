// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class HeaderReader
{
    private readonly IRedactorProvider _redactorProvider;
    private readonly KeyValuePair<string, DataClassification>[] _headers;
    private readonly string[] _normalizedHeaders;

    public HeaderReader(IDictionary<string, DataClassification> headersToLog, IRedactorProvider redactorProvider, string prefix)
    {
        _redactorProvider = redactorProvider;

        _headers = headersToLog.Count == 0 ? [] : headersToLog.ToArray();
        _normalizedHeaders = HeaderNormalizer.PrepareNormalizedHeaderNames(_headers, prefix);
    }

    public void Read(IHeaderDictionary headers, IList<KeyValuePair<string, object?>> logContext)
    {
        if (headers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _headers.Length; i++)
        {
            var header = _headers[i];

            if (headers.TryGetValue(header.Key, out var headerValue))
            {
                var provider = _redactorProvider.GetRedactor(header.Value);
                var redacted = provider.Redact(headerValue.ToString());
                logContext.Add(new(_normalizedHeaders[i], redacted));
            }
        }
    }
}
#endif
