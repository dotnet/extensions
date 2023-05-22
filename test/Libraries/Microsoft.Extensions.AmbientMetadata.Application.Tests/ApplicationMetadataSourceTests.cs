// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class ApplicationMetadataSourceTests
{
    private const string TestEnvironmentName = "fancy environment";
    private const string TestApplicationName = "fancy application";

    private readonly Mock<IHostEnvironment> _hostEnvironment = new();
    private readonly Fixture _fixture = new();

    public ApplicationMetadataSourceTests()
    {
        _hostEnvironment.Setup(h => h.EnvironmentName).Returns(TestEnvironmentName);
        _hostEnvironment.Setup(h => h.ApplicationName).Returns(TestApplicationName);
    }

    [Fact]
    public void ApplicationMetadataSource_CanConstruct() => new ApplicationMetadataSource(_hostEnvironment.Object, _fixture.Create<string>()).Should().NotBeNull();

    [Fact]
    public void ApplicationMetadataSource_NullHostEnvironment_Throws()
    {
        var act = () => new ApplicationMetadataSource(null!, _fixture.Create<string>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void ApplicationMetadataSource_InvalidSectionName_Throws(string? sectionName)
    {
        var act = () => new ApplicationMetadataSource(_hostEnvironment.Object, sectionName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ApplicationMetadataSource_Build_BuildsProviderCorrectly()
    {
        var testSectionName = _fixture.Create<string>();
        var sut = new ApplicationMetadataSource(_hostEnvironment.Object, testSectionName);
        var configurationBuilder = new ConfigurationBuilder();

        var provider = sut.Build(configurationBuilder);

        var result = provider.TryGet($"{testSectionName}:{nameof(ApplicationMetadata.EnvironmentName)}", out var actualEnvironmentName);
        result.Should().BeTrue();
        actualEnvironmentName.Should().Be(TestEnvironmentName);

        result = provider.TryGet($"{testSectionName}:{nameof(ApplicationMetadata.ApplicationName)}", out var actualApplicationName);
        result.Should().BeTrue();
        actualApplicationName.Should().Be(TestApplicationName);
    }
}
