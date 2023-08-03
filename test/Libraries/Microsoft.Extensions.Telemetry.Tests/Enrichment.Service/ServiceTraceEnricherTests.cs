// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Telemetry.Enrichment.Service.Test.Internals;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test;

public class ServiceTraceEnricherTests
{
    private const string AppName = "appNameTestValue";
    private const string EnvironmentName = "environmentTestValue";
    private const string BuildVersion = "buildVersionTestValue";
    private const string DeploymentRing = "deploymentRingTestValue";

    private readonly Mock<IHostEnvironment> _hostMock;

    public ServiceTraceEnricherTests()
    {
        _hostMock = new Mock<IHostEnvironment>(MockBehavior.Strict);
        _hostMock.SetupGet(c => c.EnvironmentName).Returns(EnvironmentName);
        _hostMock.SetupGet(c => c.ApplicationName).Returns(AppName);
    }

    [Fact]
    public void ServiceTraceEnricher_GivenEnricherOptions_Enriches()
    {
        // Arrange
        var options = new ServiceTraceEnricherOptions
        {
            ApplicationName = true,
            EnvironmentName = true,
            BuildVersion = true,
            DeploymentRing = true,
        };

        var serviceOptions = new ApplicationMetadata
        {
            BuildVersion = BuildVersion,
            DeploymentRing = DeploymentRing,
            ApplicationName = _hostMock.Object.ApplicationName,
            EnvironmentName = _hostMock.Object.EnvironmentName
        };

        var enricher = new ServiceTraceEnricher(options.ToOptions(), serviceOptions.ToOptions());
        var enrichedProperties = new TestLogEnrichmentTagCollector();
        using var activity = new Activity("test");

        // Act
        enricher.Enrich(activity);
        var enrichedState = activity.Tags.ToDictionary(static x => x.Key, static x => x.Value);

        // Assert
        Assert.Equal(AppName, enrichedState[ServiceEnricherTags.ApplicationName]);
        Assert.Equal(EnvironmentName, enrichedState[ServiceEnricherTags.EnvironmentName]);
        Assert.Equal(BuildVersion, enrichedState[ServiceEnricherTags.BuildVersion]);
        Assert.Equal(DeploymentRing, enrichedState[ServiceEnricherTags.DeploymentRing]);
    }

    [Fact]
    public void ServiceTraceEnricher_GivenDisabledEnricherOptions_DoesNotEnrich()
    {
        // Arrange
        var options = new ServiceTraceEnricherOptions
        {
            ApplicationName = false,
            EnvironmentName = false,
            BuildVersion = false,
            DeploymentRing = false,
        };

        var serviceOptions = new ApplicationMetadata
        {
            BuildVersion = BuildVersion,
            DeploymentRing = DeploymentRing
        };

        var enricher = new ServiceTraceEnricher(options.ToOptions(), serviceOptions.ToOptions());
        var enrichedProperties = new TestLogEnrichmentTagCollector();
        using var activity = new Activity("test");

        // Act
        enricher.Enrich(activity);
        IReadOnlyDictionary<string, string?> enrichedState = activity.Tags.ToDictionary(static x => x.Key, static x => x.Value);

        // Assert
        Assert.DoesNotContain<string, string?>(ServiceEnricherTags.ApplicationName, enrichedState);
        Assert.DoesNotContain<string, string?>(ServiceEnricherTags.EnvironmentName, enrichedState);
        Assert.DoesNotContain<string, string?>(ServiceEnricherTags.BuildVersion, enrichedState);
        Assert.DoesNotContain<string, string?>(ServiceEnricherTags.DeploymentRing, enrichedState);
    }
}
