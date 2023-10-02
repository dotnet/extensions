// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Routing;

public class EndpointTests
{
    [Fact]
    public void Uri_OK()
    {
        var endpoint = new UriEndpoint
        {
            Uri = new Uri("https://localhost:5001")
        };

        endpoint.Uri.Should().Be(new Uri("https://localhost:5001"));
    }
}
