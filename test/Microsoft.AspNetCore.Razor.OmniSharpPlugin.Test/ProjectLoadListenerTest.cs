// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
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
        public void TryResolveRazorConfigurationPath_NoIntermediateOutputPath_ReturnsFalse()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            var loggerFactory = Mock.Of<ILoggerFactory>();
            var projectLoadListener = new ProjectLoadListener(loggerFactory);

            // Act
            var result = ProjectLoadListener.TryResolveRazorConfigurationPath(projectInstance, out var path);

            // Assert
            Assert.False(result);
            Assert.Null(path);
        }

        [Fact]
        public void TryResolveRazorConfigurationPath_RootedIntermediateOutputPath_ReturnsTrue()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create();
            var intermediateOutputPath = "C:\\project\\obj";
            projectRootElement.AddProperty(ProjectLoadListener.IntermediateOutputPathPropertyName, intermediateOutputPath);
            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = Path.Combine(intermediateOutputPath, ProjectLoadListener.RazorConfigurationFileName);

            // Act
            var result = ProjectLoadListener.TryResolveRazorConfigurationPath(projectInstance, out var path);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedPath, path);
        }

        [Fact]
        public void TryResolveRazorConfigurationPath_RelativeIntermediateOutputPath_ReturnsTrue()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create();
            var intermediateOutputPath = "obj";
            projectRootElement.AddProperty(ProjectLoadListener.IntermediateOutputPathPropertyName, intermediateOutputPath);

            // Project directory is automatically set to the current test project (it's a reserved MSBuild property).

            var projectInstance = new ProjectInstance(projectRootElement);
            var expectedPath = Path.Combine(intermediateOutputPath, ProjectLoadListener.RazorConfigurationFileName);

            // Act
            var result = ProjectLoadListener.TryResolveRazorConfigurationPath(projectInstance, out var path);

            // Assert
            Assert.True(result);
            Assert.NotEmpty(path);
        }
    }
}
