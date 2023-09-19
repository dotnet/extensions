// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

internal readonly struct RouteSegment
{
    public RouteSegment(string segment, bool isParameter)
    {
        Segment = segment;
        IsParameter = isParameter;
    }

    public string Segment { get; }

    public bool IsParameter { get; }
}
