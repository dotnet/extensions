// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class FallbackConfigurationProviderTest
    {
        public Version MvcAssemblyVersion { get; } = Version.Parse("2.1.0");

        [Fact]
        public void TryResolveConfiguration_NoCoreCapability_ReturnsFalse()
        {
            // Arrange
            var projectCapabilities = Array.Empty<string>();
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var context = new ProjectConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new TestLegacyConfigurationProvider(MvcAssemblyVersion);

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryResolveConfiguration_RazorConfigurationCapability_ReturnsFalse()
        {
            // Arrange
            var projectCapabilities = new[] 
            {
                CoreProjectConfigurationProvider.DotNetCoreRazorCapability,
                CoreProjectConfigurationProvider.DotNetCoreRazorConfigurationCapability
            };
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var context = new ProjectConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new TestLegacyConfigurationProvider(MvcAssemblyVersion);

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryResolveConfiguration_NoMvcReference_ReturnsFalse()
        {
            // Arrange
            var context = BuildContext("/some/path/to/some.dll");
            var provider = new TestLegacyConfigurationProvider(MvcAssemblyVersion);

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryResolveConfiguration_NoMvcVersion_ReturnsFalse()
        {
            // Arrange
            var context = BuildContext("/some/path/to/some.dll", "/another/path/to/" + FallbackConfigurationProvider.MvcAssemblyFileName);
            var provider = new TestLegacyConfigurationProvider(mvcAssemblyVersion: null);

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryResolveConfiguration_MvcWithVersion_ReturnsTrue()
        {
            // Arrange
            var context = BuildContext("/some/path/to/some.dll", "/another/path/to/" + FallbackConfigurationProvider.MvcAssemblyFileName);
            var provider = new TestLegacyConfigurationProvider(MvcAssemblyVersion);
            var expectedConfiguration = FallbackRazorConfiguration.SelectConfiguration(MvcAssemblyVersion);

            // Act
            var result = provider.TryResolveConfiguration(context, out var projectConfiguration);

            // Assert
            Assert.True(result);
            Assert.Same(expectedConfiguration, projectConfiguration.Configuration);
            Assert.Empty(projectConfiguration.Documents);
        }

        private ProjectConfigurationProviderContext BuildContext(params string[] referencePaths)
        {
            var projectCapabilities = new[] { CoreProjectConfigurationProvider.DotNetCoreRazorCapability };
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            foreach (var path in referencePaths)
            {
                projectInstance.AddItem(FallbackConfigurationProvider.ReferencePathWithRefAssembliesItemType, path);
            }
            var context = new ProjectConfigurationProviderContext(projectCapabilities, projectInstance);
            return context;
        }

        private class TestLegacyConfigurationProvider : FallbackConfigurationProvider
        {
            private readonly Version _mvcAssemblyVersion;

            public TestLegacyConfigurationProvider(Version mvcAssemblyVersion)
            {
                _mvcAssemblyVersion = mvcAssemblyVersion;
            }

            protected override Version GetAssemblyVersion(string filePath) => _mvcAssemblyVersion;
        }
    }
}
