// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment.Service.Test.Internals;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Service.Test;

public class ServiceMetricEnricherTests
{
    private const string AppName = "appNameTestValue";
    private const string EnvironmentName = "environmentTestValue";
    private const string BuildVersion = "buildVersionTestValue";
    private const string DeploymentRing = "deploymentRingTestValue";

    private readonly Mock<IHostEnvironment> _hostMock;

    public ServiceMetricEnricherTests()
    {
        _hostMock = new Mock<IHostEnvironment>(MockBehavior.Strict);
        _hostMock.SetupGet(c => c.EnvironmentName).Returns(EnvironmentName);
        _hostMock.SetupGet(c => c.ApplicationName).Returns(AppName);
    }

    [Fact]
    public void ServiceMetricEnricher_GivenInvalidArguments_Throws()
    {
        // Arrange
        var options = new ServiceMetricEnricherOptions
        {
            BuildVersion = true,
            DeploymentRing = true
        }.ToOptions();
        var optionsNull = new Mock<IOptions<ServiceMetricEnricherOptions>>();
        optionsNull.Setup(o => o.Value).Returns<IOptions<ServiceMetricEnricherOptions>>(null!);

        var serviceOptionsNull = new Mock<IOptions<ApplicationMetadata>>();
        serviceOptionsNull.Setup(o => o.Value).Returns<IOptions<ApplicationMetadata>>(null!);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ServiceMetricEnricher(optionsNull.Object, null!));
        Assert.Throws<ArgumentException>(() => new ServiceMetricEnricher(options, serviceOptionsNull.Object));
    }

    [Fact]
    public void ServiceMetricEnricher_GivenEnricherOptions_Enriches()
    {
        // Arrange
        var options = new ServiceMetricEnricherOptions
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

        var enricher = new ServiceMetricEnricher(options.ToOptions(), serviceOptions.ToOptions());
        var enrichedProperties = new TestMetricEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Tags;

        // Assert
        if (options.ApplicationName)
        {
            Assert.Equal(AppName, enrichedState[ServiceEnricherTags.ApplicationName]);
        }

        if (options.EnvironmentName)
        {
            Assert.Equal(EnvironmentName, enrichedState[ServiceEnricherTags.EnvironmentName]);
        }

        if (options.BuildVersion)
        {
            Assert.Equal(BuildVersion, enrichedState[ServiceEnricherTags.BuildVersion]);
        }

        if (options.DeploymentRing)
        {
            Assert.Equal(DeploymentRing, enrichedState[ServiceEnricherTags.DeploymentRing]);
        }
    }

    [Fact]
    public void ServiceMetricEnricher_GivenDisabledEnricherOptions_DoesNotEnrich()
    {
        // Arrange
        var options = new ServiceMetricEnricherOptions
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

        var enricher = new ServiceMetricEnricher(options.ToOptions(), serviceOptions.ToOptions());
        var enrichedProperties = new TestMetricEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Tags;

        // Assert
        Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.ApplicationName));
        Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.EnvironmentName));
        Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.BuildVersion));
        Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.DeploymentRing));
    }
}
