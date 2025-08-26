// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Struct to hold the route segments created after parsing the route.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario.")]
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Preferring array for performance reasons, immutability is not a concern.")]
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly struct ParsedRouteSegments
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParsedRouteSegments"/> struct.
    /// </summary>
    /// <param name="routeTemplate">Route's template.</param>
    /// <param name="segments">Array of segments.</param>
    public ParsedRouteSegments(string routeTemplate, Segment[] segments)
    {
        _ = Throw.IfNull(segments);

        Segments = segments;

        var paramCount = 0;
        foreach (Segment segment in segments)
        {
            if (segment.IsParam)
            {
                paramCount++;
            }
        }

        ParameterCount = paramCount;
        RouteTemplate = routeTemplate;
    }

    /// <summary>
    /// Gets the route template.
    /// </summary>
    public string RouteTemplate { get; }

    /// <summary>
    /// Gets all segments of the route.
    /// </summary>
    public Segment[] Segments { get; }

    /// <summary>
    /// Gets the count of parameters in the route.
    /// </summary>
    public int ParameterCount { get; }
}
