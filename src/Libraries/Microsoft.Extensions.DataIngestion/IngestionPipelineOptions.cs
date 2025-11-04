// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

#pragma warning disable SA1500 // Braces for multi-line statements should not share line
#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <summary>
/// Options for configuring the ingestion pipeline.
/// </summary>
public sealed class IngestionPipelineOptions
{
    /// <summary>
    /// Gets or sets the name of the <see cref="ActivitySource"/> used for diagnostics.
    /// </summary>
    public string ActivitySourceName
    {
        get;
        set => field = Throw.IfNullOrEmpty(value);
    } = DiagnosticsConstants.ActivitySourceName;

    internal IngestionPipelineOptions Clone() => new()
    {
        ActivitySourceName = ActivitySourceName,
    };
}
