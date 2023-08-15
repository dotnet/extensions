// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test;

public class ServiceEnricherOptionsTests
{
    [Fact]
    public void ServiceLogEnricherOptions_EnsureDefaultValues()
    {
        var options = new ServiceLogEnricherOptions();
        options.EnvironmentName.Should().BeTrue();
        options.ApplicationName.Should().BeTrue();
        options.BuildVersion.Should().BeFalse();
        options.DeploymentRing.Should().BeFalse();
    }

    [Fact]
    public void ServiceTraceEnricherOptions_EnsureDefaultValues()
    {
        var options = new ServiceTraceEnricherOptions();
        options.EnvironmentName.Should().BeTrue();
        options.ApplicationName.Should().BeTrue();
        options.BuildVersion.Should().BeFalse();
        options.DeploymentRing.Should().BeFalse();
    }
}
