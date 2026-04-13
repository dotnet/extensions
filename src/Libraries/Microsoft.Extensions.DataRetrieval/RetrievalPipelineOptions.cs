// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Options for configuring the retrieval pipeline.
/// </summary>
public sealed class RetrievalPipelineOptions
{
    /// <summary>
    /// Gets or sets the name of the <see cref="ActivitySource"/> used for diagnostics.
    /// </summary>
    public string ActivitySourceName
    {
        get;
        set => field = Throw.IfNullOrEmpty(value);
    } = DiagnosticsConstants.ActivitySourceName;
}
