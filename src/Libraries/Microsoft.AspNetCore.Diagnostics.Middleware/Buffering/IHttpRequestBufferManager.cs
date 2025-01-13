// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

/// <summary>
/// Interface for an HTTP request buffer manager.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IHttpRequestBufferManager : IBufferManager
{
    /// <summary>
    /// Flushes the buffer and emits non-request logs.
    /// </summary>
    void FlushNonRequestLogs();

    /// <summary>
    /// Flushes the buffer and emits buffered logs for the current request.
    /// </summary>
    void FlushCurrentRequestLogs();
}
