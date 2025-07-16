// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class BuildMetadataTests
{
    [Fact]
    public void ShouldConstructObject()
    {
        var instance = new BuildMetadata();
        Assert.NotNull(instance);
    }

    [Fact]
    public void ShouldHaveAllPropertiesNull()
    {
        // Arrange
        var obj = new BuildMetadata();
        var properties = obj.GetType().GetProperties().Select(f => f.GetValue(obj)).ToArray();

        properties.Should().OnlyContain(x => x == null);
    }

    [Fact]
    public void BuildIdProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var metadata = new BuildMetadata
        {
            // Act
            BuildId = id
        };

        // Assert
        metadata.BuildId.Should().Be(id);
    }

    [Fact]
    public void BuildNumberProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new BuildMetadata
        {
            BuildNumber = "v1.2.3"
        };

        // Assert
        metadata.BuildNumber.Should().Be("v1.2.3");
    }

    [Fact]
    public void SourceBranchNameProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new BuildMetadata
        {
            SourceBranchName = "main"
        };

        // Assert
        metadata.SourceBranchName.Should().Be("main");
    }

    [Fact]
    public void SourceVersionProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var metadata = new BuildMetadata
        {
            SourceVersion = "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0"
        };

        // Assert
        metadata.SourceVersion.Should().Be("a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0");
    }

    [Fact]
    public void BuildDatetimeProperty_ShouldSetAndGetCorrectly()
    {
        // Arrange
        string dateTime = $"{DateTimeOffset.UtcNow:s}";
        var metadata = new BuildMetadata
        {
            BuildDateTime = dateTime
        };

        // Assert
        metadata.BuildDateTime.Should().Be(dateTime);
    }
}
