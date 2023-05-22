// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class ApplicationMetadataExtensionsTests
{
    private const string TestEnvironmentName = "fancy environment";
    private const string TestApplicationName = "fancy application";

    private readonly Fixture _fixture = new();
    private readonly Mock<IHostEnvironment> _hostEnvironment = new();

    public ApplicationMetadataExtensionsTests()
    {
        _hostEnvironment.Setup(h => h.EnvironmentName).Returns(TestEnvironmentName);
        _hostEnvironment.Setup(h => h.ApplicationName).Returns(TestApplicationName);
    }

    [Fact]
    public void ApplicationMetadataExtensions_GivenAnyNullArgument_Throws()
    {
        var serviceCollection = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddApplicationMetadata(config.GetSection(string.Empty)));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddApplicationMetadata(_ => { }));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.AddApplicationMetadata((Action<ApplicationMetadata>)null!));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.AddApplicationMetadata((IConfigurationSection)null!));
        Assert.Throws<ArgumentNullException>(() => ((IHostBuilder)null!).UseApplicationMetadata(_fixture.Create<string>()));
        Assert.Throws<ArgumentNullException>(() => new ConfigurationBuilder().AddApplicationMetadata(null!));
        Assert.Throws<ArgumentNullException>(() => ((IConfigurationBuilder)null!).AddApplicationMetadata(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void AddApplicationMetadata_InvalidSectionName_Throws(string? sectionName)
    {
        var act = () => new ConfigurationBuilder().AddApplicationMetadata(_hostEnvironment.Object, sectionName!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void UseApplicationMetadata_InvalidSectionName_Throws(string? sectionName)
    {
        var act = () => FakeHost.CreateBuilder().UseApplicationMetadata(sectionName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddApplicationMetadata_BuildsConfig()
    {
        var expectedConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(ApplicationMetadata)}:{nameof(ApplicationMetadata.ApplicationName)}"] = TestApplicationName,
                [$"{nameof(ApplicationMetadata)}:{nameof(ApplicationMetadata.EnvironmentName)}"] = TestEnvironmentName,
            })
            .Build();
        var expectedConfigSection = expectedConfig.GetSection(nameof(ApplicationMetadata));

        var actualConfig = new ConfigurationBuilder().AddApplicationMetadata(_hostEnvironment.Object, nameof(ApplicationMetadata)).Build();
        var actualConfigSection = actualConfig.GetSection(nameof(ApplicationMetadata));

        actualConfigSection.Should().BeEquivalentTo(expectedConfigSection);
    }

    [Fact]
    public void AddApplicationMetadata_GivenConfigurationSection_RegistersMetadata()
    {
        var expectedMetadata = new ApplicationMetadata
        {
            ApplicationName = _fixture.Create<string>(),
            EnvironmentName = _fixture.Create<string>(),
            BuildVersion = _fixture.Create<string>(),
            DeploymentRing = _fixture.Create<string>(),
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(ApplicationMetadata)}:{nameof(ApplicationMetadata.ApplicationName)}"] = expectedMetadata.ApplicationName,
                [$"{nameof(ApplicationMetadata)}:{nameof(ApplicationMetadata.EnvironmentName)}"] = expectedMetadata.EnvironmentName,
                [$"{nameof(ApplicationMetadata)}:{nameof(ApplicationMetadata.BuildVersion)}"] = expectedMetadata.BuildVersion,
                [$"{nameof(ApplicationMetadata)}:{nameof(ApplicationMetadata.DeploymentRing)}"] = expectedMetadata.DeploymentRing,
            })
            .Build();

        var configurationSection = config.GetSection(nameof(ApplicationMetadata));

        using var provider = new ServiceCollection()
            .AddApplicationMetadata(configurationSection)
            .BuildServiceProvider();

        var actualMetadata = provider.GetRequiredService<IOptions<ApplicationMetadata>>().Value;

        actualMetadata.Should().BeEquivalentTo(expectedMetadata);
    }

    [Fact]
    public void AddApplicationMetadata_GivenConfigurationDelegate_RegistersMetadata()
    {
        var expectedMetadata = new ApplicationMetadata
        {
            ApplicationName = _fixture.Create<string>(),
            EnvironmentName = _fixture.Create<string>(),
            BuildVersion = _fixture.Create<string>(),
            DeploymentRing = _fixture.Create<string>(),
        };

        using var provider = new ServiceCollection()
            .AddApplicationMetadata(m =>
            {
                m.ApplicationName = expectedMetadata.ApplicationName;
                m.EnvironmentName = expectedMetadata.EnvironmentName;
                m.BuildVersion = expectedMetadata.BuildVersion;
                m.DeploymentRing = expectedMetadata.DeploymentRing;
            })
            .BuildServiceProvider();

        var actualMetadata = provider.GetRequiredService<IOptions<ApplicationMetadata>>().Value;

        actualMetadata.Should().BeEquivalentTo(expectedMetadata);
    }
}
