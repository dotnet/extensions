// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    /// Gets or sets the duration to check and remove the buffered items exceeding the <see cref="PerRequestCapacity"/>.
    /// </summary>
    public TimeSpan PerRequestDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the size of the buffer for a request.
    /// </summary>
    public int PerRequestCapacity { get; set; } = 1_000;

#pragma warning disable CA1002 // Do not expose generic lists - List is necessary to be able to call .AddRange()
#pragma warning disable CA2227 // Collection properties should be read only - setter is necessary for options pattern
    /// <summary>
    /// Gets or sets the collection of <see cref="BufferFilterRule"/> used for filtering log messages for the purpose of further buffering.
    /// </summary>
    public List<BufferFilterRule> Rules { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists
}
