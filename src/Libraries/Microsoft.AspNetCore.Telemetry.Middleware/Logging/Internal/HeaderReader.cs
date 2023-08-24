// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal sealed class HeaderReader
{
    private readonly IRedactorProvider _redactorProvider;
    private readonly KeyValuePair<string, DataClassification>[] _headers;

    public HeaderReader(IDictionary<string, DataClassification> headersToLog, IRedactorProvider redactorProvider)
    {
        _redactorProvider = redactorProvider;

        _headers = headersToLog.Count == 0
            ? Array.Empty<KeyValuePair<string, DataClassification>>()
            : headersToLog.ToArray();
    }

    public void Read(IHeaderDictionary headers, IList<KeyValuePair<string, object?>> logContext, string prefix)
    {
        if (headers.Count == 0)
        {
            return;
        }

        foreach (var header in _headers)
        {
            if (headers.TryGetValue(header.Key, out var headerValue))
            {
                var provider = _redactorProvider.GetRedactor(header.Value);
                var redacted = provider.Redact(headerValue.ToString());
                logContext.Add(new(prefix + header.Key, redacted));
            }
        }
    }
}
#endif
