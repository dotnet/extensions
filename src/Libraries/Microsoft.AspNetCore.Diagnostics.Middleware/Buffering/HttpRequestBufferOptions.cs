// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

/// <summary>
/// The options for LoggerBuffer.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class HttpRequestBufferOptions
{
    /// <summary>
    /// Gets or sets the size in bytes of the buffer for a request. If the buffer size exceeds this limit, the oldest buffered log records will be dropped.
    /// </summary>
    /// TO DO: add validation.
    public int PerRequestBufferSizeInBytes { get; set; } = 5_000_000;

#pragma warning disable CA1002 // Do not expose generic lists - List is necessary to be able to call .AddRange()
    /// <summary>
    /// Gets the collection of <see cref="BufferFilterRule"/> used for filtering log messages for the purpose of further buffering.
    /// </summary>
    public List<BufferFilterRule> Rules { get; } = [];
#pragma warning restore CA1002 // Do not expose generic lists
}
