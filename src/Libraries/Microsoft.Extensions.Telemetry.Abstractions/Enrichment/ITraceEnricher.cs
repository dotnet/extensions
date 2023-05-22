// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// A component that augments tracing state with additional tags.
/// </summary>
public interface ITraceEnricher
{
    /// <summary>
    /// Called to let the component add tags to a tracing activity.
    /// </summary>
    /// <param name="activity">The activity to add the tags to.</param>
    void Enrich(Activity activity);

    /// <summary>
    /// Called to let the component add tags to the start event of a tracing activity.
    /// </summary>
    /// <param name="activity">The activity to add the tags to.</param>
    void EnrichOnActivityStart(Activity activity);
}
