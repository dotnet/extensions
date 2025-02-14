// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

/// <summary>
/// Buffers HTTP request logs into circular buffers and drops them after some time if not flushed.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class HttpRequestLogBuffer : LogBuffer
{
    /// <summary>
    /// Flushes buffers and emits buffered logs for the current HTTP request.
    /// </summary>
    public abstract void FlushCurrentRequestLogs();
}
