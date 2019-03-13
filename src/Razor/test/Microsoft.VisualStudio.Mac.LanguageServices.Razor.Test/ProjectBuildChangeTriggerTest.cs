// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Editor.Razor.Test;
using MonoDevelop.Projects;
using Moq;
using Xunit;
using Project = Microsoft.CodeAnalysis.Project;
using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    public class ProjectBuildChangeTriggerTest : ForegroundDispatcherTestBase
    {
        public ProjectBuildChangeTriggerTest()
        {
            SomeProject = new HostProject("c:\\SomeProject\\SomeProject.csproj", FallbackRazorConfiguration.MVC_1_0, "SomeProject");
            SomeOtherProject = new HostProject("c:\\SomeOtherProject\\SomeOtherProject.csproj", FallbackRazorConfiguration.MVC_2_0, "SomeOtherProject");

            Workspace = TestWorkspace.Create(w =>
            {
                SomeWorkspaceProject = w.AddProject(ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "SomeProject",
                    "SomeProject",
                    LanguageNames.CSharp,
                    filePath: SomeProject.FilePath));
            });
        }

        private HostProject SomeProject { get; }

        private HostProject SomeOtherProject { get; }

        private Project SomeWorkspaceProject { get; set; }

        private Workspace Workspace { get; }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_EnqueuesProjectStateUpdate()
        {
            // Arrange
            var expectedProjectPath = SomeProject.FilePath;
            var projectService = CreateProjectService(expectedProjectPath);

            var args = new BuildEventArgs(monitor: null, success: true);
            var projectSnapshot = new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeProject));

            var projectManager = new Mock<ProjectSnapshotManagerBase>(MockBehavior.Strict);
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(SomeProject.FilePath))
                .Returns(projectSnapshot);
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, workspaceStateGenerator, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(SomeWorkspaceProject, update.workspaceProject);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_ProjectWithoutWorkspaceProject_Noops()
        {
            // Arrange
            var expectedPath = "Path/To/Project.csproj";
            var projectService = CreateProjectService(expectedPath);

            var args = new BuildEventArgs(monitor: null, success: true);
            var projectSnapshot = new DefaultProjectSnapshot(
                ProjectState.Create(
                    Workspace.Services,
                    new HostProject(expectedPath, RazorConfiguration.Default, "Project")));

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedPath))
                .Returns(projectSnapshot);
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, workspaceStateGenerator, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            Assert.Empty(workspaceStateGenerator.UpdateQueue);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_UntrackedProject_Noops()
        {
            // Arrange
            var projectService = CreateProjectService(SomeProject.FilePath);

            var args = new BuildEventArgs(monitor: null, success: true);

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(SomeProject.FilePath))
                .Returns<ProjectSnapshot>(null);
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService, workspaceStateGenerator, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            Assert.Empty(workspaceStateGenerator.UpdateQueue);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_BuildFailed_Noops()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: false);
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.IsSupportedProject(null)).Throws<InvalidOperationException>();
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Throws<InvalidOperationException>();
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService.Object, workspaceStateGenerator, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            Assert.Empty(workspaceStateGenerator.UpdateQueue);
        }

        [ForegroundFact]
        public void ProjectOperations_EndBuild_UnsupportedProject_Noops()
        {
            // Arrange
            var args = new BuildEventArgs(monitor: null, success: true);
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.IsSupportedProject(null)).Returns(false);
            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Throws<InvalidOperationException>();
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var trigger = new ProjectBuildChangeTrigger(Dispatcher, projectService.Object, workspaceStateGenerator, projectManager.Object);

            // Act
            trigger.ProjectOperations_EndBuild(null, args);

            // Assert
            Assert.Empty(workspaceStateGenerator.UpdateQueue);
        }

        private static TextBufferProjectService CreateProjectService(string projectPath)
        {
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(null)).Returns(projectPath);
            projectService.Setup(p => p.IsSupportedProject(null)).Returns(true);
            return projectService.Object;
        }
    }
}
