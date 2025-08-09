// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class AzureVmMetadataConfigBuilderExtensionsTests
{
    [Fact]
    public void GivenNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IConfigurationBuilder? builder = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddAzureVmMetadata());
    }

    [Fact]
    public void GivenNullSectionName_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigurationBuilder builder = new();

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => builder.AddAzureVmMetadata(sectionName: null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GivenWhitespaceSectionName_ThrowsArgumentException(string sectionName)
    {
        // Arrange
        ConfigurationBuilder builder = new();

        // Act and Assert
        Assert.Throws<ArgumentException>(() => builder.AddAzureVmMetadata(sectionName: sectionName));
    }
}
