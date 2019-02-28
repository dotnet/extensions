// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmnisharpPlugin
{
    public class MSBuildProjectManagerTest
    {
        [Fact]
        public void GetRazorConfiguration_ProvidersReturnsTrue_ReturnsConfig()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(MSBuildProjectManager.ProjectCapabilityItemType, CoreProjectConfigurationProvider.DotNetCoreRazorCapability);
            var provider1 = new Mock<RazorConfigurationProvider>();
            var configuration = RazorConfiguration.Default; // Setting to non-null to ensure the listener doesn't return the config verbatim.
            provider1.Setup(p => p.TryResolveConfiguration(It.IsAny<RazorConfigurationProviderContext>(), out configuration))
                .Returns(false);
            var provider2 = new Mock<RazorConfigurationProvider>();
            provider2.Setup(p => p.TryResolveConfiguration(It.IsAny<RazorConfigurationProviderContext>(), out configuration))
                .Returns(true);

            // Act
            var result = MSBuildProjectManager.GetRazorConfiguration(projectInstance, new[] { provider1.Object, provider2.Object });

            // Assert
            Assert.Same(configuration, result);
        }

        [Fact]
        public void GetRazorConfiguration_SingleProviderReturnsFalse_ReturnsNull()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(MSBuildProjectManager.ProjectCapabilityItemType, CoreProjectConfigurationProvider.DotNetCoreRazorCapability);
            var provider = new Mock<RazorConfigurationProvider>();
            var configuration = RazorConfiguration.Default; // Setting to non-null to ensure the listener doesn't return the config verbatim.
            provider.Setup(p => p.TryResolveConfiguration(It.IsAny<RazorConfigurationProviderContext>(), out configuration))
                .Returns(false);

            // Act
            var result = MSBuildProjectManager.GetRazorConfiguration(projectInstance, Enumerable.Empty<RazorConfigurationProvider>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRazorConfiguration_NoProviders_ReturnsNull()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(MSBuildProjectManager.ProjectCapabilityItemType, CoreProjectConfigurationProvider.DotNetCoreRazorCapability);

            // Act
            var result = MSBuildProjectManager.GetRazorConfiguration(projectInstance, Enumerable.Empty<RazorConfigurationProvider>());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryResolveConfigurationOutputPath_MSBuildIntermediateOutputPath_Normalizes()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create();

            // Note the ending \ here that gets normalized away.
            var intermediateOutputPath = "C:/project\\obj";
            projectRootElement.AddProperty(MSBuildProjectManager.IntermediateOutputPathPropertyName, intermediateOutputPath);
            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = string.Format("C:{0}project{0}obj{0}{1}", Path.DirectorySeparatorChar, MSBuildProjectManager.RazorConfigurationFileName);

            // Act
            var result = MSBuildProjectManager.TryResolveConfigurationOutputPath(projectInstance, out var path);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedPath, path);
        }

        [Fact]
        public void TryResolveConfigurationOutputPath_NoIntermediateOutputPath_ReturnsFalse()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var loggerFactory = Mock.Of<ILoggerFactory>();

            // Act
            var result = MSBuildProjectManager.TryResolveConfigurationOutputPath(projectInstance, out var path);

            // Assert
            Assert.False(result);
            Assert.Null(path);
        }

        [Fact]
        public void TryResolveConfigurationOutputPath_RootedIntermediateOutputPath_ReturnsTrue()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create();
            var intermediateOutputPath = string.Format("C:{0}project{0}obj", Path.DirectorySeparatorChar);
            projectRootElement.AddProperty(MSBuildProjectManager.IntermediateOutputPathPropertyName, intermediateOutputPath);
            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = Path.Combine(intermediateOutputPath, MSBuildProjectManager.RazorConfigurationFileName);

            // Act
            var result = MSBuildProjectManager.TryResolveConfigurationOutputPath(projectInstance, out var path);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedPath, path);
        }

        [Fact]
        public void TryResolveConfigurationOutputPath_RelativeIntermediateOutputPath_ReturnsTrue()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create();
            var intermediateOutputPath = "obj";
            projectRootElement.AddProperty(MSBuildProjectManager.IntermediateOutputPathPropertyName, intermediateOutputPath);

            // Project directory is automatically set to the current test project (it's a reserved MSBuild property).

            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = Path.Combine(intermediateOutputPath, MSBuildProjectManager.RazorConfigurationFileName);

            // Act
            var result = MSBuildProjectManager.TryResolveConfigurationOutputPath(projectInstance, out var path);

            // Assert
            Assert.True(result);
            Assert.NotEmpty(path);
        }
    }
}
