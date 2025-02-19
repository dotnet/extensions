// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

/// <summary>
/// The options for HTTP request log buffering.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public class HttpRequestLogBufferingOptions
{
    /// <summary>
    /// Gets or sets the size in bytes of the buffer for a request. If the buffer size exceeds this limit, the oldest buffered log records will be dropped.
    /// </summary>
    /// TO DO: add validation.
    public int MaxPerRequestBufferSizeInBytes { get; set; } = 5_000_000;

#pragma warning disable CA2227 // Collection properties should be read only - setter is necessary for options pattern
    /// <summary>
    /// Gets or sets the collection of <see cref="LogBufferingFilterRule"/> used for filtering log messages for the purpose of further buffering.
    /// </summary>
    public IList<LogBufferingFilterRule> Rules { get; set; } = [];
#pragma warning restore CA2227
}
