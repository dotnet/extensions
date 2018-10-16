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
    public class ProjectLoadListenerTest
    {
        [Fact]
        public void GetTargetFramework_ReturnsTargetFramework()
        {
            // Arrange
            var expectedTFM = "net461";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty(ProjectLoadListener.TargetFrameworkPropertyName, expectedTFM);

            // Act
            var tfm = ProjectLoadListener.GetTargetFramework(projectInstance);

            // Assert
            Assert.Equal(expectedTFM, tfm);
        }

        [Fact]
        public void GetTargetFramework_NoTFM_ReturnsTargetFrameworkVersion()
        {
            // Arrange
            var expectedTFM = "v4.6.1";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty(ProjectLoadListener.TargetFrameworkVersionPropertyName, expectedTFM);

            // Act
            var tfm = ProjectLoadListener.GetTargetFramework(projectInstance);

            // Assert
            Assert.Equal(expectedTFM, tfm);
        }

        [Fact]
        public void GetTargetFramework_PrioritizesTargetFrameworkOverVersion()
        {
            // Arrange
            var expectedTFM = "v4.6.1";
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.SetProperty(ProjectLoadListener.TargetFrameworkPropertyName, expectedTFM);
            projectInstance.SetProperty(ProjectLoadListener.TargetFrameworkVersionPropertyName, "Unexpected");

            // Act
            var tfm = ProjectLoadListener.GetTargetFramework(projectInstance);

            // Assert
            Assert.Equal(expectedTFM, tfm);
        }

        [Fact]
        public void GetTargetFramework_NoTFM_ReturnsEmpty()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());

            // Act
            var tfm = ProjectLoadListener.GetTargetFramework(projectInstance);

            // Assert
            Assert.Empty(tfm);
        }

        [Fact]
        public void GetRazorConfiguration_ProvidersReturnsTrue_ReturnsConfig()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(ProjectLoadListener.ProjectCapabilityItemType, CoreProjectConfigurationProvider.DotNetCoreRazorCapability);
            var loggerFactory = Mock.Of<ILoggerFactory>();
            var provider1 = new Mock<RazorConfigurationProvider>();
            var configuration = RazorConfiguration.Default; // Setting to non-null to ensure the listener doesn't return the config verbatim.
            provider1.Setup(p => p.TryResolveConfiguration(It.IsAny<RazorConfigurationProviderContext>(), out configuration))
                .Returns(false);
            var provider2 = new Mock<RazorConfigurationProvider>();
            provider2.Setup(p => p.TryResolveConfiguration(It.IsAny<RazorConfigurationProviderContext>(), out configuration))
                .Returns(true);
            var projectLoadListener = new ProjectLoadListener(new[] { provider1.Object, provider2.Object }, loggerFactory);

            // Act
            var result = projectLoadListener.GetRazorConfiguration(projectInstance);

            // Assert
            Assert.Same(configuration, result);
        }

        [Fact]
        public void GetRazorConfiguration_SingleProviderReturnsFalse_ReturnsNull()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(ProjectLoadListener.ProjectCapabilityItemType, CoreProjectConfigurationProvider.DotNetCoreRazorCapability);
            var loggerFactory = Mock.Of<ILoggerFactory>();
            var provider = new Mock<RazorConfigurationProvider>();
            var configuration = RazorConfiguration.Default; // Setting to non-null to ensure the listener doesn't return the config verbatim.
            provider.Setup(p => p.TryResolveConfiguration(It.IsAny<RazorConfigurationProviderContext>(), out configuration))
                .Returns(false);
            var projectLoadListener = new ProjectLoadListener(Enumerable.Empty<RazorConfigurationProvider>(), loggerFactory);

            // Act
            var result = projectLoadListener.GetRazorConfiguration(projectInstance);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetRazorConfiguration_NoProviders_ReturnsNull()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            projectInstance.AddItem(ProjectLoadListener.ProjectCapabilityItemType, CoreProjectConfigurationProvider.DotNetCoreRazorCapability);
            var loggerFactory = Mock.Of<ILoggerFactory>();
            var projectLoadListener = new ProjectLoadListener(Enumerable.Empty<RazorConfigurationProvider>(), loggerFactory);

            // Act
            var result = projectLoadListener.GetRazorConfiguration(projectInstance);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryResolveConfigurationOutputPath_NoIntermediateOutputPath_ReturnsFalse()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var loggerFactory = Mock.Of<ILoggerFactory>();

            // Act
            var result = ProjectLoadListener.TryResolveConfigurationOutputPath(projectInstance, out var path);

            // Assert
            Assert.False(result);
            Assert.Null(path);
        }

        [Fact]
        public void TryResolveConfigurationOutputPath_RootedIntermediateOutputPath_ReturnsTrue()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create();
            var intermediateOutputPath = "C:\\project\\obj";
            projectRootElement.AddProperty(ProjectLoadListener.IntermediateOutputPathPropertyName, intermediateOutputPath);
            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = Path.Combine(intermediateOutputPath, ProjectLoadListener.RazorConfigurationFileName);

            // Act
            var result = ProjectLoadListener.TryResolveConfigurationOutputPath(projectInstance, out var path);

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
            projectRootElement.AddProperty(ProjectLoadListener.IntermediateOutputPathPropertyName, intermediateOutputPath);

            // Project directory is automatically set to the current test project (it's a reserved MSBuild property).

            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = Path.Combine(intermediateOutputPath, ProjectLoadListener.RazorConfigurationFileName);

            // Act
            var result = ProjectLoadListener.TryResolveConfigurationOutputPath(projectInstance, out var path);

            // Assert
            Assert.True(result);
            Assert.NotEmpty(path);
        }
    }
}
