// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Diagnostics.Enrichment.Test.Internals;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Enrichment.Test;

public class ApplicationLogEnricherTests
{
    private const string AppName = "appNameTestValue";
    private const string EnvironmentName = "environmentTestValue";
    private const string BuildVersion = "buildVersionTestValue";
    private const string DeploymentRing = "deploymentRingTestValue";

    private readonly Mock<IHostEnvironment> _hostMock;

    public ApplicationLogEnricherTests()
    {
        _hostMock = new Mock<IHostEnvironment>(MockBehavior.Strict);
        _hostMock.SetupGet(c => c.EnvironmentName).Returns(EnvironmentName);
        _hostMock.SetupGet(c => c.ApplicationName).Returns(AppName);
    }

    [Fact]
    public void HostLogEnricher_GivenInvalidArguments_Throws()
    {
        // Arrange
        var options = new ApplicationLogEnricherOptions
        {
            BuildVersion = true,
            DeploymentRing = true
        }.ToOptions();
        var optionsNull = new Mock<IOptions<ApplicationLogEnricherOptions>>();
        optionsNull.Setup(o => o.Value).Returns<IOptions<ApplicationLogEnricherOptions>>(null!);

        var serviceOptionsNull = new Mock<IOptions<ApplicationMetadata>>();
        serviceOptionsNull.Setup(o => o.Value).Returns<IOptions<ApplicationMetadata>>(null!);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ApplicationLogEnricher(optionsNull.Object, null!));
        Assert.Throws<ArgumentException>(() => new ApplicationLogEnricher(options, serviceOptionsNull.Object));
    }

    [Theory]
    [InlineData(true, true, true, true, null, null)]
    [InlineData(true, true, true, true, BuildVersion, DeploymentRing)]
    [InlineData(false, false, false, false, null, null)]
    [InlineData(false, false, false, false, BuildVersion, DeploymentRing)]
    public void ServiceLogEnricher_Options(bool appName, bool envName, bool buildVer, bool depRing, string? buildVersion, string? deploymentRing)
    {
        // Arrange
        var options = new ApplicationLogEnricherOptions
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

        var enricher = new ApplicationLogEnricher(options.ToOptions(), serviceOptions.ToOptions());
        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Tags;

        // Assert
        if (appName)
        {
            Assert.Equal(AppName, enrichedState[ApplicationEnricherTags.ApplicationName]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ApplicationEnricherTags.ApplicationName));
        }

        if (envName)
        {
            Assert.Equal(EnvironmentName, enrichedState[ApplicationEnricherTags.EnvironmentName]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ApplicationEnricherTags.EnvironmentName));
        }

        if (buildVer && buildVersion != null)
        {
            Assert.Equal(BuildVersion, enrichedState[ApplicationEnricherTags.BuildVersion]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ApplicationEnricherTags.BuildVersion));
        }

        if (depRing && deploymentRing != null)
        {
            Assert.Equal(DeploymentRing, enrichedState[ApplicationEnricherTags.DeploymentRing]);
        }
        else
        {
            Assert.False(enrichedState.ContainsKey(ApplicationEnricherTags.DeploymentRing));
        }
    }
}
