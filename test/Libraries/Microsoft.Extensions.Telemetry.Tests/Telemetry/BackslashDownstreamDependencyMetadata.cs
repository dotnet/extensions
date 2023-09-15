// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Telemetry.Telemetry;

internal sealed class BackslashDownstreamDependencyMetadata : IDownstreamDependencyMetadata
{
    private static readonly ISet<string> _uniqueHostNameSuffixes = new HashSet<string>
    {
        "anotherservice.net",
    };

    private static readonly ISet<RequestMetadata> _requestMetadataSet = new HashSet<RequestMetadata>
    {
        new ("DELETE", "/singlebackslash", "StartingSingleBackslash"),
        new ("POST", "//doublebackslash", "StartingDoublebackslash"),
        new ("PUT", "/singlethensingle/", "StartingSingleBackslashEndingSingleBackslash"),
        new ("GET", "//doublethensingle/", "StartingDoublebackslashEndingSingleBackslash"),
    };

    public string DependencyName => "BackslashService";

    public ISet<string> UniqueHostNameSuffixes => _uniqueHostNameSuffixes;

    public ISet<RequestMetadata> RequestMetadata => _requestMetadataSet;
}
