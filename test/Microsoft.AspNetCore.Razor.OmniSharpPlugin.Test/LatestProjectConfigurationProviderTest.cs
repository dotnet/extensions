// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class LatestProjectConfigurationProviderTest
    {
        [Fact]
        public void GetRootNamespace_NoRootNamespace_ReturnsNull()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());

            // Act
            var rootNamespace = LatestProjectConfigurationProvider.GetRootNamespace(projectInstance);

            // Assert
            Assert.Null(rootNamespace);
        }

        [Fact]
        public void GetRootNamespace_EmptyRootNamespace_ReturnsNull()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty(LatestProjectConfigurationProvider.RootNamespaceProperty, string.Empty);

            // Act
            var rootNamespace = LatestProjectConfigurationProvider.GetRootNamespace(projectInstance);

            // Assert
            Assert.Null(rootNamespace);
        }

        [Fact]
        public void GetRootNamespace_ReturnsRootNamespace()
        {
            // Arrange
            var expectedRootNamespace = "SomeApp.Root.Namespace";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty(LatestProjectConfigurationProvider.RootNamespaceProperty, expectedRootNamespace);

            // Act
            var rootNamespace = LatestProjectConfigurationProvider.GetRootNamespace(projectInstance);

            // Assert
            Assert.Equal(expectedRootNamespace, rootNamespace);
        }

        [Fact]
        public void GetHostDocuments_SomeLegacyDocuments()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(LatestProjectConfigurationProvider.RazorGenerateWithTargetPathItemType, "file.cshtml", new Dictionary<string, string>()
            {
                [LatestProjectConfigurationProvider.RazorTargetPathMetadataName] = "path/file.cshtml",
            });
            projectInstance.AddItem(LatestProjectConfigurationProvider.RazorGenerateWithTargetPathItemType, "otherfile.cshtml", new Dictionary<string, string>()
            {
                [LatestProjectConfigurationProvider.RazorTargetPathMetadataName] = "other/path/otherfile.cshtml",
            });

            // Act
            var hostDocuments = LatestProjectConfigurationProvider.GetHostDocuments(projectInstance.Items);

            // Assert
            Assert.Collection(
                hostDocuments,
                hostDocument =>
                {
                    Assert.Equal("file.cshtml", hostDocument.FilePath);
                    Assert.Equal("path/file.cshtml", hostDocument.TargetPath);
                    Assert.Equal(FileKinds.Legacy, hostDocument.FileKind);
                },
                hostDocument =>
                {
                    Assert.Equal("otherfile.cshtml", hostDocument.FilePath);
                    Assert.Equal("other/path/otherfile.cshtml", hostDocument.TargetPath);
                    Assert.Equal(FileKinds.Legacy, hostDocument.FileKind);
                });
        }

        [Fact]
        public void GetHostDocuments_SomeComponentDocuments()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(LatestProjectConfigurationProvider.RazorComponentWithTargetPathItemType, "file.razor", new Dictionary<string, string>()
            {
                [LatestProjectConfigurationProvider.RazorTargetPathMetadataName] = "path/file.razor",
            });
            projectInstance.AddItem(LatestProjectConfigurationProvider.RazorComponentWithTargetPathItemType, "otherfile.razor", new Dictionary<string, string>()
            {
                [LatestProjectConfigurationProvider.RazorTargetPathMetadataName] = "other/path/otherfile.razor",
            });

            // Act
            var hostDocuments = LatestProjectConfigurationProvider.GetHostDocuments(projectInstance.Items);

            // Assert
            Assert.Collection(
                hostDocuments,
                hostDocument =>
                {
                    Assert.Equal("file.razor", hostDocument.FilePath);
                    Assert.Equal("path/file.razor", hostDocument.TargetPath);
                    Assert.Equal(FileKinds.Component, hostDocument.FileKind);
                },
                hostDocument =>
                {
                    Assert.Equal("otherfile.razor", hostDocument.FilePath);
                    Assert.Equal("other/path/otherfile.razor", hostDocument.TargetPath);
                    Assert.Equal(FileKinds.Component, hostDocument.FileKind);
                });
        }

        [Fact]
        public void GetHostDocuments_MixedDocuments()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(LatestProjectConfigurationProvider.RazorComponentWithTargetPathItemType, "file.razor", new Dictionary<string, string>()
            {
                [LatestProjectConfigurationProvider.RazorTargetPathMetadataName] = "path/file.razor",
            });
            projectInstance.AddItem(LatestProjectConfigurationProvider.RazorGenerateWithTargetPathItemType, "otherfile.cshtml", new Dictionary<string, string>()
            {
                [LatestProjectConfigurationProvider.RazorTargetPathMetadataName] = "other/path/otherfile.cshtml",
            });

            // Act
            var hostDocuments = LatestProjectConfigurationProvider.GetHostDocuments(projectInstance.Items);

            // Assert
            Assert.Collection(
                hostDocuments,
                hostDocument =>
                {
                    Assert.Equal("file.razor", hostDocument.FilePath);
                    Assert.Equal("path/file.razor", hostDocument.TargetPath);
                    Assert.Equal(FileKinds.Component, hostDocument.FileKind);
                },
                hostDocument =>
                {
                    Assert.Equal("otherfile.cshtml", hostDocument.FilePath);
                    Assert.Equal("other/path/otherfile.cshtml", hostDocument.TargetPath);
                    Assert.Equal(FileKinds.Legacy, hostDocument.FileKind);
                });
        }

        [Fact]
        public void TryResolveConfiguration_NoCoreCapability_ReturnsFalse()
        {
            // Arrange
            var projectCapabilities = Array.Empty<string>();
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var context = new ProjectConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new LatestProjectConfigurationProvider();

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
            var context = new ProjectConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new LatestProjectConfigurationProvider();

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
            var result = LatestProjectConfigurationProvider.TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration);

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
            var result = LatestProjectConfigurationProvider.TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration);

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
            var result = LatestProjectConfigurationProvider.TryGetDefaultConfiguration(projectInstance, out var defaultConfiguration);

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
            var result = LatestProjectConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

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
            var result = LatestProjectConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

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
            var result = LatestProjectConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

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
            var result = LatestProjectConfigurationProvider.TryGetLanguageVersion(projectInstance, out var languageVersion);

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
            var result = LatestProjectConfigurationProvider.TryGetConfigurationItem("Razor-13.37", projectItems, out var configurationItem);

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
            var result = LatestProjectConfigurationProvider.TryGetConfigurationItem("Razor-13.37", projectInstance.Items, out var configurationItem);

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
            var result = LatestProjectConfigurationProvider.TryGetConfigurationItem(expectedConfiguration, projectInstance.Items, out var configurationItem);

            // Assert
            Assert.True(result);
            Assert.Same(expectedConfigurationItem, configurationItem);
        }

        [Fact]
        public void GetConfiguredExtensionNames_NoExtensions()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var configurationItem = projectInstance.AddItem("RazorConfiguration", "Razor-10.0");

            // Act
            var configuredExtensionNames = LatestProjectConfigurationProvider.GetConfiguredExtensionNames(configurationItem);

            // Assert
            Assert.Empty(configuredExtensionNames);
        }

        [Fact]
        public void GetConfiguredExtensionNames_EmptyExtensions()
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
            var configuredExtensionNames = LatestProjectConfigurationProvider.GetConfiguredExtensionNames(configurationItem);

            // Assert
            Assert.Empty(configuredExtensionNames);
        }

        [Fact]
        public void GetConfiguredExtensionNames_SingleExtension()
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
            var configuredExtensionNames = LatestProjectConfigurationProvider.GetConfiguredExtensionNames(configurationItem);

            // Assert
            var extensionName = Assert.Single(configuredExtensionNames);
            Assert.Equal(expectedExtensionName, extensionName);
        }

        [Fact]
        public void GetConfiguredExtensionNames_MultipleExtensions()
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
            var configuredExtensionNames = LatestProjectConfigurationProvider.GetConfiguredExtensionNames(configurationItem);

            // Assert
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
            var extensions = LatestProjectConfigurationProvider.GetExtensions(new[] { "Extension1", "Extension2" }, projectInstance.Items);

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
            var extensions = LatestProjectConfigurationProvider.GetExtensions(new[] { "Extension1", "Extension2" }, projectInstance.Items);

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
            var extensions = LatestProjectConfigurationProvider.GetExtensions(new[] { expectedExtension1Name, expectedExtension2Name }, projectInstance.Items);

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
            var result = LatestProjectConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

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

            // Act
            var result = LatestProjectConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

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

            // Act
            var result = LatestProjectConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryGetConfiguration_SucceedsIfNoConfiguredExtensionNames()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty("RazorDefaultConfiguration", "Razor-13.37");
            projectInstance.SetProperty("RazorLangVersion", "1.0");
            projectInstance.AddItem("RazorConfiguration", "Razor-13.37");

            // Act
            var result = LatestProjectConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.True(result);
            Assert.Equal(RazorLanguageVersion.Version_1_0, configuration.Configuration.LanguageVersion);
            Assert.Equal("Razor-13.37", configuration.Configuration.ConfigurationName);
            Assert.Empty(configuration.Configuration.Extensions);
        }

        // This is more of an integration test but is here to test the overall flow/functionality
        [Fact]
        public void TryGetConfiguration_SucceedsWithAllPreRequisites()
        {
            // Arrange
            var expectedRootNamespace = "SomeApp.Root.Namespace";
            var expectedLanguageVersion = RazorLanguageVersion.Version_1_0;
            var expectedConfigurationName = "Razor-Test";
            var expectedExtension1Name = "Extension1";
            var expectedExtension2Name = "Extension2";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty(LatestProjectConfigurationProvider.RootNamespaceProperty, expectedRootNamespace);
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
            var result = LatestProjectConfigurationProvider.TryGetConfiguration(projectInstance, out var configuration);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedLanguageVersion, configuration.Configuration.LanguageVersion);
            Assert.Equal(expectedConfigurationName, configuration.Configuration.ConfigurationName);
            Assert.Collection(
                configuration.Configuration.Extensions,
                extension => Assert.Equal(expectedExtension1Name, extension.ExtensionName),
                extension => Assert.Equal(expectedExtension2Name, extension.ExtensionName));
            Assert.Equal(expectedRootNamespace, configuration.RootNamespace);
        }
    }
}
