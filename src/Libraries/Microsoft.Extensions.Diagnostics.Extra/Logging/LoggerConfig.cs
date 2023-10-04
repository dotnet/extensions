// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.Logging;

internal sealed class LoggerConfig
{
    public LoggerConfig(
        KeyValuePair<string, object?>[] staticTags,
        Action<IEnrichmentTagCollector>[] enrichers,
        bool captureStackTraces,
        bool useFileInfoForStackTraces,
        bool includeExceptionMessagesInStackTraces,
        int maxStackTraceLength,
        Func<IReadOnlyList<DataClassification>, Redactor> getRedactor)
    {
        StaticTags = staticTags;
        Enrichers = enrichers;
        CaptureStackTraces = captureStackTraces;
        UseFileInfoForStackTraces = useFileInfoForStackTraces;
        MaxStackTraceLength = maxStackTraceLength;
        IncludeExceptionMessageInStackTraces = includeExceptionMessagesInStackTraces;
        GetRedactor = getRedactor;
    }

    public KeyValuePair<string, object?>[] StaticTags { get; }
    public Action<IEnrichmentTagCollector>[] Enrichers { get; }
    public bool CaptureStackTraces { get; }
    public bool UseFileInfoForStackTraces { get; }
    public bool IncludeExceptionMessageInStackTraces { get; }
    public int MaxStackTraceLength { get; }
    public Func<IReadOnlyList<DataClassification>, Redactor> GetRedactor { get; }
}
