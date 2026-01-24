// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class BuildMetadataServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenAnyNullArgument_ShouldThrowArgumentNullException()
    {
        var serviceCollection = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        serviceCollection.Invoking(x => ((IServiceCollection)null!).AddBuildMetadata(config.GetSection(string.Empty)))
            .Should().Throw<ArgumentNullException>();

        serviceCollection.Invoking(x => x.AddBuildMetadata((Action<BuildMetadata>)null!))
            .Should().Throw<ArgumentNullException>();

        serviceCollection.Invoking(x => ((IServiceCollection)null!).AddBuildMetadata(_ => { }))
            .Should().Throw<ArgumentNullException>();

        serviceCollection.Invoking(x => x.AddBuildMetadata((IConfigurationSection)null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenConfigurationSection_ShouldRegisterMetadataFromIt()
    {
        // Arrange
        var testData = new BuildMetadata
        {
            BuildId = Guid.NewGuid().ToString(),
            BuildNumber = "v1.2.3",
            SourceBranchName = "main",
            SourceVersion = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0",
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(BuildMetadata)}:{nameof(BuildMetadata.BuildId)}"] = testData.BuildId,
                [$"{nameof(BuildMetadata)}:{nameof(BuildMetadata.BuildNumber)}"] = testData.BuildNumber,
                [$"{nameof(BuildMetadata)}:{nameof(BuildMetadata.SourceBranchName)}"] = testData.SourceBranchName,
                [$"{nameof(BuildMetadata)}:{nameof(BuildMetadata.SourceVersion)}"] = testData.SourceVersion,
            })
            .Build();

        var configurationSection = config
            .GetSection(nameof(BuildMetadata));

        // Act
        using var provider = new ServiceCollection()
            .AddBuildMetadata(configurationSection)
            .BuildServiceProvider();
        var metadata = provider
            .GetRequiredService<IOptions<BuildMetadata>>().Value;

        // Assert
        metadata.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void GivenActionDelegate_ShouldRegisterMetadataFromIt()
    {
        // Arrange
        var testData = new BuildMetadata
        {
            BuildId = Guid.NewGuid().ToString(),
            BuildNumber = "v1.2.3",
            SourceBranchName = "main",
            SourceVersion = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0",
        };

        // Act
        using var provider = new ServiceCollection()
            .AddBuildMetadata(m =>
            {
                m.BuildId = testData.BuildId;
                m.BuildNumber = testData.BuildNumber;
                m.SourceVersion = testData.SourceVersion;
                m.SourceBranchName = testData.SourceBranchName;
            })
            .BuildServiceProvider();
        var metadata = provider
            .GetRequiredService<IOptions<BuildMetadata>>().Value;

        // Assert
        metadata.Should().BeEquivalentTo(testData);
    }
}
