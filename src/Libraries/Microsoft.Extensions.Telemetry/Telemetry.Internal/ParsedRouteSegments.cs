// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Struct to hold the route segments created after parsing the route.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types
internal readonly struct ParsedRouteSegments
#pragma warning restore CA1815 // Override equals and operator equals on value types
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
        foreach (var segment in segments)
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
#pragma warning disable CA1819 // Properties should not return arrays
    public Segment[] Segments { get; }
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>
    /// Gets the count of parameters in the route.
    /// </summary>
    public int ParameterCount { get; }
}

