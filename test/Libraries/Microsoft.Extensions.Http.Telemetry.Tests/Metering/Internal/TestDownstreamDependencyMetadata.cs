// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal sealed class TestDownstreamDependencyMetadata : IDownstreamDependencyMetadata
{
    public string DependencyName => "testdep";

    private static readonly ISet<string> _uniqueHostNameSuffixes = new HashSet<string>
    {
        ".test.com",
    };

    private static readonly ISet<RequestMetadata> _requestMetadataSet = new HashSet<RequestMetadata>
    {
        new ("GET", "testroute", "testrequestname"),
    };

    public ISet<string> UniqueHostNameSuffixes => _uniqueHostNameSuffixes;

    public ISet<RequestMetadata> RequestMetadata => _requestMetadataSet;
}
