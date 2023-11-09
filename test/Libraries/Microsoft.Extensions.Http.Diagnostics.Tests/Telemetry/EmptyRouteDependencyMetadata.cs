// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Telemetry;

internal sealed class EmptyRouteDependencyMetadata : IDownstreamDependencyMetadata
{
    private static readonly ISet<string> _uniqueHostNameSuffixes = new HashSet<string>
    {
        "emptyroute.com",
    };

    private static readonly ISet<RequestMetadata> _requestMetadataSet = new HashSet<RequestMetadata>
    {
        new ("GET", "", "EmptyRoute"),
        new ("GET", "a", "PathWithoutBackslash")
    };

    public string DependencyName => "EmptyRouteService";

    public ISet<string> UniqueHostNameSuffixes => _uniqueHostNameSuffixes;

    public ISet<RequestMetadata> RequestMetadata => _requestMetadataSet;
}

