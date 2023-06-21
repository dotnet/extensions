// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed class LoggerConfig
{
    public LoggerConfig(
        KeyValuePair<string, object?>[] staticProperties,
        Action<IEnrichmentPropertyBag>[] enrichers,
        bool captureStackTraces,
        bool useFileInfoForStackTraces,
        int maxStackTraceLength,
        Func<DataClassification, Redactor> redactorProvider)
    {
        StaticProperties = staticProperties;
        Enrichers = enrichers;
        CaptureStackTraces = captureStackTraces;
        UseFileInfoForStackTraces = useFileInfoForStackTraces;
        MaxStackTraceLength = maxStackTraceLength;
        RedactorProvider = redactorProvider;
    }

    public KeyValuePair<string, object?>[] StaticProperties { get; }
    public Action<IEnrichmentPropertyBag>[] Enrichers { get; }
    public bool CaptureStackTraces { get; }
    public bool UseFileInfoForStackTraces { get; }
    public int MaxStackTraceLength { get; }
    public Func<DataClassification, Redactor> RedactorProvider { get; }
}
