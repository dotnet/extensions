// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class LatestConfigurationProviderTest
    {
        [Fact]
        public void TryResolveConfiguration_NoCoreCapability_ReturnsFalse()
        {
            // Arrange
            var projectCapabilities = Array.Empty<string>();
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var context = new RazorConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new LatestConfigurationProvider();

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryResolveConfiguration_NoRazorConfigurationCapability_ReturnsFalse()
        {
            // Arrange
            var projectCapabilities = new[]
            {
                CoreProjectConfigurationProvider.DotNetCoreRazorCapability,
            };
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var context = new RazorConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new LatestConfigurationProvider();

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_FailsIfNoConfiguration()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());

            // Act
            var result = LatestConfigurationProvider.TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_FailsIfEmptyConfiguration()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorDefaultConfiguration", string.Empty);

            // Act
            var result = LatestConfigurationProvider.TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration);

            // Assert
            Assert.False(result);
            Assert.Null(defaultConfiguration);
        }

        [Fact]
        public void TryGetDefaultConfiguration_SucceedsWithValidConfiguration()
        {
            // Arrange
            var expectedConfiguration = "Razor-13.37";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorDefaultConfiguration", expectedConfiguration);

            // Act
            var result = LatestConfigurationProvider.TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedConfiguration, defaultConfiguration);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfNoLanguageVersion()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());

            // Act
            var result = LatestConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_FailsIfEmptyLanguageVersion()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorLangVersion", string.Empty);

            // Act
            var result = LatestConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

            // Assert
            Assert.False(result);
            Assert.Null(languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_SucceedsWithValidLanguageVersion()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorLangVersion", "1.0");

            // Act
            var result = LatestConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Version_1_0, languageVersion);
        }

        [Fact]
        public void TryGetLanguageVersion_SucceedsWithUnknownLanguageVersion_DefaultsToLatest()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorLangVersion", "13.37");

            // Act
            var result = LatestConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

            // Assert
            Assert.True(result);
            Assert.Same(RazorLanguageVersion.Latest, languageVersion);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoRazorConfigurationItems()
        {
            // Arrange
            var projectItems = Enumerable.Empty<ProjectItemInstance>();

            // Act
            var result = LatestConfigurationProvider.TryGetConfigurationItem("Razor-13.37", projectItems, out var configurationItem);

            // Assert
            Assert.False(result);
            Assert.Null(configurationItem);
        }

        [Fact]
        public void TryGetConfigurationItem_FailsNoMatchingRazorConfigurationItems()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem("RazorConfiguration", "Razor-10.0");

            // Act
            var result = LatestConfigurationProvider.TryGetConfigurationItem("Razor-13.37", projectInstance.Items, out var configurationItem);

            // Assert
            Assert.False(result);
            Assert.Null(configurationItem);
        }

        [Fact]
        public void TryGetConfigurationItem_SucceedsForMatchingConfigurationItem()
        {
            // Arrange
            var expectedConfiguration = "Razor-13.37";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem("RazorConfiguration", "Razor-10.0-DoesNotMatch");
            var expectedConfigurationItem = projectInstance.AddItem("RazorConfiguration", expectedConfiguration);

            // Act
            var result = LatestConfigurationProvider.TryGetConfigurationItem(expectedConfiguration, projectInstance.Items, out var configurationItem);

            // Assert
            Assert.True(result);
            Assert.Same(expectedConfigurationItem, configurationItem);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_FailsIfNoExtensions()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var configurationItem = projectInstance.AddItem("RazorConfiguration", "Razor-10.0");

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionnames);

            // Assert
            Assert.False(result);
            Assert.Null(configuredExtensionnames);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_FailsIfEmptyExtensions()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var configurationItem = projectInstance.AddItem(
                "RazorConfiguration",
                "Razor-10.0",
                new Dictionary<string, string>()
                {
                    ["Extensions"] = string.Empty
                });

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames);

            // Assert
            Assert.False(result);
            Assert.Null(configuredExtensionNames);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_SucceedsIfSingleExtension()
        {
            // Arrange
            var expectedExtensionName = "SomeExtensionName";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var configurationItem = projectInstance.AddItem(
                "RazorConfiguration",
                "Razor-10.0",
                new Dictionary<string, string>()
                {
                    ["Extensions"] = expectedExtensionName
                });

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames);

            // Assert
            Assert.True(result);
            var extensionName = Assert.Single(configuredExtensionNames);
            Assert.Equal(expectedExtensionName, extensionName);
        }

        [Fact]
        public void TryGetConfiguredExtensionNames_SucceedsIfMultipleExtensions()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var configurationItem = projectInstance.AddItem(
                "RazorConfiguration",
                "Razor-10.0",
                new Dictionary<string, string>()
                {
                    ["Extensions"] = "SomeExtensionName;SomeOtherExtensionName"
                });

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguredExtensionNames(configurationItem, out var configuredExtensionNames);

            // Assert
            Assert.True(result);
            Assert.Collection(
                configuredExtensionNames,
                name => Assert.Equal("SomeExtensionName", name),
                name => Assert.Equal("SomeOtherExtensionName", name));
        }

        [Fact]
        public void GetExtensions_NoExtensionTypes_ReturnsEmptyArray()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem("NotAnExtension", "Extension1");

            // Act
            var extensions = LatestConfigurationProvider.GetExtensions(new[] { "Extension1", "Extension2" }, projectInstance.Items);

            // Assert
            Assert.Empty(extensions);
        }

        [Fact]
        public void GetExtensions_UnConfiguredExtensionTypes_ReturnsEmptyArray()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem("NotAnExtension", "UnconfiguredExtensionName");

            // Act
            var extensions = LatestConfigurationProvider.GetExtensions(new[] { "Extension1", "Extension2" }, projectInstance.Items);

            // Assert
            Assert.Empty(extensions);
        }

        [Fact]
        public void GetExtensions_SomeConfiguredExtensions_ReturnsConfiguredExtensions()
        {
            // Arrange
            var expectedExtension1Name = "Extension1";
            var expectedExtension2Name = "Extension2";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem("NotAnExtension", "UnconfiguredExtensionName");
            projectInstance.AddItem("RazorExtension", expectedExtension1Name);
            projectInstance.AddItem("RazorExtension", expectedExtension2Name);

            // Act
            var extensions = LatestConfigurationProvider.GetExtensions(new[] { expectedExtension1Name, expectedExtension2Name }, projectInstance.Items);

            // Assert
            Assert.Collection(
                extensions,
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName));
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoDefaultConfiguration()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoLanguageVersion()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorDefaultConfiguration", "Razor-13.37");
            var projectItems = new ProjectItemInstance[0];

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoConfigurationItems()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorDefaultConfiguration", "Razor-13.37");
            projectInstance.SetProperty("RazorLangVersion", "1.0");
            var projectItems = new ProjectItemInstance[0];

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_FailsIfNoConfiguredExtensionNames()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorDefaultConfiguration", "Razor-13.37");
            projectInstance.SetProperty("RazorLangVersion", "1.0");
            projectInstance.AddItem("RazorConfiguration", "Razor-13.37");

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        // This is more of an integration test but is here to test the overall flow/functionality
        [Fact]
        public void TryGetConfiguration_SucceedsWithAllPreRequisites()
        {
            // Arrange
            var expectedLanguageVersion = RazorLanguageVersion.Version_1_0;
            var expectedConfigurationName = "Razor-Test";
            var expectedExtension1Name = "Extension1";
            var expectedExtension2Name = "Extension2";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem("RazorConfiguration", "UnconfiguredRazorConfiguration");
            projectInstance.AddItem("RazorConfiguration", "UnconfiguredExtensionName");
            projectInstance.AddItem("RazorExtension", expectedExtension1Name);
            projectInstance.AddItem("RazorExtension", expectedExtension2Name);
            var expectedRazorConfigurationItem = projectInstance.AddItem(
                "RazorConfiguration", 
                expectedConfigurationName, 
                new Dictionary<string, string>()
                {
                    ["Extensions"] = "Extension1;Extension2",
                });

            projectInstance.SetProperty("RazorDefaultConfiguration", expectedConfigurationName);
            projectInstance.SetProperty("RazorLangVersion", "1.0");

            // Act
            var result = LatestConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedLanguageVersion, configuration.LanguageVersion);
            Assert.Equal(expectedConfigurationName, configuration.ConfigurationName);
            Assert.Collection(
                configuration.Extensions,
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName));
        }
    }
}
