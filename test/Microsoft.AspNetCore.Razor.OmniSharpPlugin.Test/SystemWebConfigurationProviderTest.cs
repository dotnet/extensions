// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class SystemWebConfigurationProviderTest
    {
        [Fact]
        public void TryResolveConfiguration_RazorCoreCapability_ReturnsFalse()
        {
            // Arrange
            var projectCapabilities = new[]
            {
                CoreProjectConfigurationProvider.DotNetCoreRazorCapability,
            };
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(SystemWebConfigurationProvider.ReferencePathWithRefAssembliesItemType, SystemWebConfigurationProvider.SystemWebRazorAssemblyFileName);
            var context = new RazorConfigurationProviderContext(projectCapabilities, projectInstance);
            var provider = new SystemWebConfigurationProvider();

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.False(result);
            Assert.Null(configuration);
        }

        [Fact]
        public void TryResolveConfiguration_NoSystemWebRazorReference_ReturnsFalse()
        {
            // Arrange
            var context = BuildContext("/some/path/to/some.dll");
            var provider = new SystemWebConfigurationProvider();

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
            var context = BuildContext("/some/path/to/some.dll", "/another/path/to/" + SystemWebConfigurationProvider.SystemWebRazorAssemblyFileName);
            var provider = new SystemWebConfigurationProvider();

            // Act
            var result = provider.TryResolveConfiguration(context, out var configuration);

            // Assert
            Assert.True(result);
            Assert.Same(UnsupportedRazorConfiguration.Instance, configuration);
        }

        private RazorConfigurationProviderContext BuildContext(params string[] referencePaths)
        {
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            foreach (var path in referencePaths)
            {
                projectInstance.AddItem(SystemWebConfigurationProvider.ReferencePathWithRefAssembliesItemType, path);
            }
            var context = new RazorConfigurationProviderContext(Array.Empty<string>(), projectInstance);
            return context;
        }
    }
}
