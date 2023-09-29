// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Enrichment.Test;

public class ApplicationEnricherOptionsTests
{
    [Fact]
    public void ServiceLogEnricherOptions_EnsureDefaultValues()
    {
        var options = new ApplicationLogEnricherOptions();
        options.EnvironmentName.Should().BeTrue();
        options.ApplicationName.Should().BeTrue();
        options.BuildVersion.Should().BeFalse();
        options.DeploymentRing.Should().BeFalse();
    }
}
