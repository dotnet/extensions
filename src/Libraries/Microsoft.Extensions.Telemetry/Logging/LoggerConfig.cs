// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Diagnostics.Logging.Buffering;
using Microsoft.Extensions.Diagnostics.Logging.Sampling;

namespace Microsoft.Extensions.Logging;

internal sealed class LoggerConfig
{
#pragma warning disable S107 // Methods should not have too many parameters
    public LoggerConfig(
        KeyValuePair<string, object?>[] staticTags,
        Action<IEnrichmentTagCollector>[] enrichers,
        LoggerSampler[] samplers,
        bool captureStackTraces,
        bool useFileInfoForStackTraces,
        bool includeExceptionMessage,
        int maxStackTraceLength,
        Func<DataClassificationSet, Redactor> getRedactor,
        bool addRedactionDiscriminator,
        ILoggingBufferProvider bufferProvider)
    {
#pragma warning restore S107 // Methods should not have too many parameters
        StaticTags = staticTags;
        Enrichers = enrichers;
        Samplers = samplers;
        CaptureStackTraces = captureStackTraces;
        UseFileInfoForStackTraces = useFileInfoForStackTraces;
        MaxStackTraceLength = maxStackTraceLength;
        IncludeExceptionMessage = includeExceptionMessage;
        GetRedactor = getRedactor;
        AddRedactionDiscriminator = addRedactionDiscriminator;
        BufferProvider = bufferProvider;
    }

    public KeyValuePair<string, object?>[] StaticTags { get; }
    public Action<IEnrichmentTagCollector>[] Enrichers { get; }
    public LoggerSampler[] Samplers { get; }
    public bool CaptureStackTraces { get; }
    public bool UseFileInfoForStackTraces { get; }
    public bool IncludeExceptionMessage { get; }
    public int MaxStackTraceLength { get; }
    public Func<DataClassificationSet, Redactor> GetRedactor { get; }
    public bool AddRedactionDiscriminator { get; }
    public ILoggingBufferProvider BufferProvider { get; }
}
