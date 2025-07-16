// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.BuildMetadata.Test;

public class BuildMetadataHostBuilderExtensionsTests
{
    [Fact]
    public void Verify_HostBuilder()
    {
        var host = FakeHost.CreateBuilder()
            .UseBuildMetadata()
            .Build();

        var buildMetadata = host.Services.GetRequiredService<IOptions<Extensions.AmbientMetadata.BuildMetadata>>();

        buildMetadata.Value.Should().NotBeNull();
        buildMetadata.Value.BuildId.Should().Be(BuildMetadataValues.BuildId);
        buildMetadata.Value.BuildNumber.Should().Be(BuildMetadataValues.BuildNumber);
        buildMetadata.Value.SourceBranchName.Should().Be(BuildMetadataValues.SourceBranchName);
        buildMetadata.Value.SourceVersion.Should().Be(BuildMetadataValues.SourceVersion);
        buildMetadata.Value.BuildDateTime.Should().Be(BuildMetadataValues.BuildDateTime);
    }

    [Fact]
    public void Verify_ApplicationHostBuilder()
    {
        var host = Host.CreateApplicationBuilder()
            .UseBuildMetadata()
            .Build();

        var buildMetadata = host.Services.GetRequiredService<IOptions<Extensions.AmbientMetadata.BuildMetadata>>();

        buildMetadata.Value.Should().NotBeNull();
        buildMetadata.Value.BuildId.Should().Be(BuildMetadataValues.BuildId);
        buildMetadata.Value.BuildNumber.Should().Be(BuildMetadataValues.BuildNumber);
        buildMetadata.Value.SourceBranchName.Should().Be(BuildMetadataValues.SourceBranchName);
        buildMetadata.Value.SourceVersion.Should().Be(BuildMetadataValues.SourceVersion);
        buildMetadata.Value.BuildDateTime.Should().Be(BuildMetadataValues.BuildDateTime);
    }
}
