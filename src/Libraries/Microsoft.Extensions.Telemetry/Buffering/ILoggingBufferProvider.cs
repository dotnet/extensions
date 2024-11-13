// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Interface providing access to the current logging buffer.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public interface ILoggingBufferProvider
{
    /// <summary>
    /// Gets current logging buffer.
    /// </summary>
    public ILoggingBuffer CurrentBuffer { get; }
}
#endif
