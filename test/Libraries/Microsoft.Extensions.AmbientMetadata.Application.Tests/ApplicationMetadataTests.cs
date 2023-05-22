// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AutoFixture;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class ApplicationMetadataTests
{
    private readonly ApplicationMetadata _sut;
    private readonly Fixture _fixture;

    public ApplicationMetadataTests()
    {
        _sut = new ApplicationMetadata();
        _fixture = new Fixture();
    }

    [Fact]
    public void CanConstruct() => new ApplicationMetadata().Should().NotBeNull();

    [Fact]
    public void DefaultChecks()
    {
        var applicationMetadata = new ApplicationMetadata();

        applicationMetadata.ApplicationName.Should().BeEmpty();
        applicationMetadata.EnvironmentName.Should().BeEmpty();
        applicationMetadata.BuildVersion.Should().BeNull();
        applicationMetadata.DeploymentRing.Should().BeNull();
    }

    [Fact]
    public void ApplicationMetadata_ApplicationName_CanSetAndGet()
    {
        var testValue = _fixture.Create<string>();

        _sut.ApplicationName = testValue;

        _sut.ApplicationName.Should().Be(testValue);
    }

    [Fact]
    public void ApplicationMetadata_EnvironmentName_CanSetAndGet()
    {
        var testValue = _fixture.Create<string>();

        _sut.EnvironmentName = testValue;

        _sut.EnvironmentName.Should().Be(testValue);
    }

    [Fact]
    public void ApplicationMetadata_BuildVersion_CanSetAndGet()
    {
        var testValue = _fixture.Create<string>();

        _sut.BuildVersion = testValue;

        _sut.BuildVersion.Should().Be(testValue);
    }

    [Fact]
    public void ApplicationMetadata_DeploymentRing_CanSetAndGet()
    {
        var testValue = _fixture.Create<string>();

        _sut.DeploymentRing = testValue;

        _sut.DeploymentRing.Should().Be(testValue);
    }
}
