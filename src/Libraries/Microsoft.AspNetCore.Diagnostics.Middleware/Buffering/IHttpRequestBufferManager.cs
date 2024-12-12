// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.Buffering;

/// <summary>
/// Interface for a HTTP request buffer manager.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public interface IHttpRequestBufferManager : IBufferManager
{
    /// <summary>
    /// Flushes the buffer and emits buffered logs for the current request.
    /// </summary>
    public void FlushCurrentRequestLogs();
}
#endif
