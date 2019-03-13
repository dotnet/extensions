// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.LanguageServices.Razor.Test;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class VsSolutionUpdatesProjectSnapshotChangeTriggerTest
    {
        public VsSolutionUpdatesProjectSnapshotChangeTriggerTest()
        {
            SomeProject = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_1_0, TestProjectData.SomeProject.RootNamespace);
            SomeOtherProject = new HostProject(TestProjectData.AnotherProject.FilePath, FallbackRazorConfiguration.MVC_2_0, TestProjectData.AnotherProject.RootNamespace);

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

        [Fact]
        public void Initialize_AttachesEventSink()
        {
            // Arrange
            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK)
                .Verifiable();

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(
                services.Object,
                Mock.Of<TextBufferProjectService>(),
                Mock.Of<ProjectWorkspaceStateGenerator>());

            // Act
            trigger.Initialize(Mock.Of<ProjectSnapshotManagerBase>());

            // Assert
            buildManager.Verify();
        }

        [Fact]
        public void UpdateProjectCfg_Done_KnownProject_EnqueuesProjectStateUpdate()
        {
            // Arrange
            var expectedProjectPath = SomeProject.FilePath;

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var projectSnapshots = new[]
            {
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeProject)),
                new DefaultProjectSnapshot(ProjectState.Create(Workspace.Services, SomeOtherProject)),
            };

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedProjectPath))
                .Returns(projectSnapshots[0]);
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object, workspaceStateGenerator);
            trigger.Initialize(projectManager.Object);

            // Act
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);

            // Assert
            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(update.workspaceProject.Id, SomeWorkspaceProject.Id);
            Assert.Same(update.projectSnapshot, projectSnapshots[0]);
        }

        [Fact]
        public void UpdateProjectCfg_Done_WithoutWorkspaceProject_DoesNotEnqueueUpdate()
        {
            // Arrange
            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);
            var projectSnapshot = new DefaultProjectSnapshot(
                ProjectState.Create(
                    Workspace.Services, 
                    new HostProject("/Some/Unknown/Path.csproj", RazorConfiguration.Default, "Path")));
            var expectedProjectPath = projectSnapshot.FilePath;

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedProjectPath))
                .Returns(projectSnapshot);
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object, workspaceStateGenerator);
            trigger.Initialize(projectManager.Object);

            // Act
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);

            // Assert
            Assert.Empty(workspaceStateGenerator.UpdateQueue);
        }

        [Fact]
        public void UpdateProjectCfg_Done_UnknownProject_DoesNotEnqueueUpdate()
        {
            // Arrange
            var expectedProjectPath = "Path/To/Project";

            uint cookie;
            var buildManager = new Mock<IVsSolutionBuildManager>(MockBehavior.Strict);
            buildManager
                .Setup(b => b.AdviseUpdateSolutionEvents(It.IsAny<VsSolutionUpdatesProjectSnapshotChangeTrigger>(), out cookie))
                .Returns(VSConstants.S_OK);

            var services = new Mock<IServiceProvider>();
            services.Setup(s => s.GetService(It.Is<Type>(f => f == typeof(SVsSolutionBuildManager)))).Returns(buildManager.Object);

            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(p => p.GetProjectPath(It.IsAny<IVsHierarchy>())).Returns(expectedProjectPath);

            var projectManager = new Mock<ProjectSnapshotManagerBase>();
            projectManager.SetupGet(p => p.Workspace).Returns(Workspace);
            projectManager
                .Setup(p => p.GetLoadedProject(expectedProjectPath))
                .Returns((ProjectSnapshot)null);
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();

            var trigger = new VsSolutionUpdatesProjectSnapshotChangeTrigger(services.Object, projectService.Object, workspaceStateGenerator);
            trigger.Initialize(projectManager.Object);

            // Act
            trigger.UpdateProjectCfg_Done(Mock.Of<IVsHierarchy>(), Mock.Of<IVsCfg>(), Mock.Of<IVsCfg>(), 0, 0, 0);

            // Assert
            Assert.Empty(workspaceStateGenerator.UpdateQueue);
        }
    }
}
