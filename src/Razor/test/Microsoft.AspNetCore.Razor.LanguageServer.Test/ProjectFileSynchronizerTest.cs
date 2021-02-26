// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class ProjectFileSynchronizerTest : LanguageServerTestBase
    {
        [Fact]
        public void ProjectFileChanged_Added_AddsProject()
        {
            // Arrange
            var projectPath = "/path/to/project.csproj";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(projectPath)).Verifiable();
            var synchronizer = new ProjectFileSynchronizer(Dispatcher, projectService.Object);

            // Act
            synchronizer.ProjectFileChanged(projectPath, RazorFileChangeKind.Added);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectFileChanged_Removed_RemovesProject()
        {
            // Arrange
            var projectPath = "/path/to/project.csproj";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.RemoveProject(projectPath)).Verifiable();
            var synchronizer = new ProjectFileSynchronizer(Dispatcher, projectService.Object);

            // Act
            synchronizer.ProjectFileChanged(projectPath, RazorFileChangeKind.Removed);

            // Assert
            projectService.VerifyAll();
        }
    }
}
