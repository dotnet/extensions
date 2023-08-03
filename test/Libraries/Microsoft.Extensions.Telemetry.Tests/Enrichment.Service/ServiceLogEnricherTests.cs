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

public class ServiceLogEnricherTests
{
    private const string AppName = "appNameTestValue";
    private const string EnvironmentName = "environmentTestValue";
    private const string BuildVersion = "buildVersionTestValue";
    private const string DeploymentRing = "deploymentRingTestValue";

    private readonly Mock<IHostEnvironment> _hostMock;

    public ServiceLogEnricherTests()
    {
        _hostMock = new Mock<IHostEnvironment>(MockBehavior.Strict);
        _hostMock.SetupGet(c => c.EnvironmentName).Returns(EnvironmentName);
        _hostMock.SetupGet(c => c.ApplicationName).Returns(AppName);
    }

    [Fact]
    public void HostLogEnricher_GivenInvalidArguments_Throws()
    {
        // Arrange
        var options = new ServiceLogEnricherOptions
        {
            BuildVersion = true,
            DeploymentRing = true
        }.ToOptions();
        var optionsNull = new Mock<IOptions<ServiceLogEnricherOptions>>();
        optionsNull.Setup(o => o.Value).Returns<IOptions<ServiceLogEnricherOptions>>(null!);

        var serviceOptionsNull = new Mock<IOptions<ApplicationMetadata>>();
        serviceOptionsNull.Setup(o => o.Value).Returns<IOptions<ApplicationMetadata>>(null!);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ServiceLogEnricher(optionsNull.Object, null!));
        Assert.Throws<ArgumentException>(() => new ServiceLogEnricher(options, serviceOptionsNull.Object));
    }

    [Theory]
    [InlineData(true, true, true, true, null, null)]
    [InlineData(true, true, true, true, BuildVersion, DeploymentRing)]
    [InlineData(false, false, false, false, null, null)]
    [InlineData(false, false, false, false, BuildVersion, DeploymentRing)]
    public void ServiceLogEnricher_Options(bool appName, bool envName, bool buildVer, bool depRing, string? buildVersion, string? deploymentRing)
    {
        // Arrange
        var options = new ServiceLogEnricherOptions
        {
            ApplicationName = appName,
            EnvironmentName = envName,
            BuildVersion = buildVer,
            DeploymentRing = depRing,
        };

        var serviceOptions = new ApplicationMetadata
        {
            BuildVersion = buildVersion,
            DeploymentRing = deploymentRing,
            ApplicationName = _hostMock.Object.ApplicationName,
            EnvironmentName = _hostMock.Object.EnvironmentName
        };

        var enricher = new ServiceLogEnricher(options.ToOptions(), serviceOptions.ToOptions());
        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Tags;

        // Assert
        if (appName)
        {
            Assert.Equal(AppName, enrichedState[ServiceEnricherTags.ApplicationName]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.ApplicationName));
        }

        if (envName)
        {
            Assert.Equal(EnvironmentName, enrichedState[ServiceEnricherTags.EnvironmentName]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.EnvironmentName));
        }

        if (buildVer && buildVersion != null)
        {
            Assert.Equal(BuildVersion, enrichedState[ServiceEnricherTags.BuildVersion]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.BuildVersion));
        }

        if (depRing && deploymentRing != null)
        {
            Assert.Equal(DeploymentRing, enrichedState[ServiceEnricherTags.DeploymentRing]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ServiceEnricherTags.DeploymentRing));
        }
    }
}
