// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class ProjectSnapshotSynchronizationServiceTest : WorkspaceTestBase
    {
        public ProjectSnapshotSynchronizationServiceTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskFactory = new JoinableTaskFactory(joinableTaskContext.Context);

            SessionContext = new TestCollaborationSession(isHost: false);

            ProjectSnapshotManager = new TestProjectSnapshotManager(Workspace);

            ProjectWorkspaceStateWithTagHelpers = new ProjectWorkspaceState(new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly").Build()
            }, default);
        }

        private JoinableTaskFactory JoinableTaskFactory { get; }

        private CollaborationSession SessionContext { get; }

        private TestProjectSnapshotManager ProjectSnapshotManager { get; }

        private ProjectWorkspaceState ProjectWorkspaceStateWithTagHelpers { get; }

        [Fact]
        public async Task InitializeAsync_RetrievesHostProjectManagerStateAndInitializesGuestManager()
        {
            // Arrange
            var projectHandle = new ProjectSnapshotHandleProxy(
                new Uri("vsls:/path/project.csproj"),
                RazorConfiguration.Default,
                "project",
                ProjectWorkspaceStateWithTagHelpers);
            var state = new ProjectSnapshotManagerProxyState(new[] { projectHandle });
            var hostProjectManagerProxy = Mock.Of<IProjectSnapshotManagerProxy>(
                proxy => proxy.GetProjectManagerStateAsync(It.IsAny<CancellationToken>()) == Task.FromResult(state));
            var synchronizationService = new ProjectSnapshotSynchronizationService(
                JoinableTaskFactory,
                SessionContext,
                hostProjectManagerProxy,
                ProjectSnapshotManager);

            // Act
            await synchronizationService.InitializeAsync(CancellationToken.None);

            // Assert
            var project = Assert.Single(ProjectSnapshotManager.Projects);
            Assert.Equal("/guest/path/project.csproj", project.FilePath);
            Assert.Same(RazorConfiguration.Default, project.Configuration);
            Assert.Same(ProjectWorkspaceStateWithTagHelpers.TagHelpers, project.TagHelpers);
        }

        [Fact]
        public void UpdateGuestProjectManager_ProjectAdded()
        {
            // Arrange
            var newHandle = new ProjectSnapshotHandleProxy(
                new Uri("vsls:/path/project.csproj"),
                RazorConfiguration.Default,
                "project",
                ProjectWorkspaceStateWithTagHelpers);
            var synchronizationService = new ProjectSnapshotSynchronizationService(
                JoinableTaskFactory,
                SessionContext,
                Mock.Of<IProjectSnapshotManagerProxy>(),
                ProjectSnapshotManager);
            var args = new ProjectChangeEventProxyArgs(older: null, newHandle, ProjectProxyChangeKind.ProjectAdded);

            // Act
            synchronizationService.UpdateGuestProjectManager(args);

            // Assert
            var project = Assert.Single(ProjectSnapshotManager.Projects);
            Assert.Equal("/guest/path/project.csproj", project.FilePath);
            Assert.Same(RazorConfiguration.Default, project.Configuration);
            Assert.Same(ProjectWorkspaceStateWithTagHelpers.TagHelpers, project.TagHelpers);
        }

        [Fact]
        public void UpdateGuestProjectManager_ProjectRemoved()
        {
            // Arrange
            var olderHandle = new ProjectSnapshotHandleProxy(
                new Uri("vsls:/path/project.csproj"),
                RazorConfiguration.Default,
                "project",
                projectWorkspaceState: null);
            var synchronizationService = new ProjectSnapshotSynchronizationService(
                JoinableTaskFactory,
                SessionContext,
                Mock.Of<IProjectSnapshotManagerProxy>(),
                ProjectSnapshotManager);
            var hostProject = new HostProject("/guest/path/project.csproj", RazorConfiguration.Default, "project");
            ProjectSnapshotManager.ProjectAdded(hostProject);
            var args = new ProjectChangeEventProxyArgs(olderHandle, newer: null, ProjectProxyChangeKind.ProjectRemoved);

            // Act
            synchronizationService.UpdateGuestProjectManager(args);

            // Assert
            Assert.Empty(ProjectSnapshotManager.Projects);
        }

        [Fact]
        public void UpdateGuestProjectManager_ProjectChanged_ConfigurationChange()
        {
            // Arrange
            var oldHandle = new ProjectSnapshotHandleProxy(
                new Uri("vsls:/path/project.csproj"),
                RazorConfiguration.Default,
                "project",
                projectWorkspaceState: null);
            var newConfiguration = RazorConfiguration.Create(RazorLanguageVersion.Version_1_0, "Custom-1.0", Enumerable.Empty<RazorExtension>());
            var newHandle = new ProjectSnapshotHandleProxy(
                oldHandle.FilePath,
                newConfiguration,
                oldHandle.RootNamespace,
                oldHandle.ProjectWorkspaceState);
            var synchronizationService = new ProjectSnapshotSynchronizationService(
                JoinableTaskFactory,
                SessionContext,
                Mock.Of<IProjectSnapshotManagerProxy>(),
                ProjectSnapshotManager);
            var hostProject = new HostProject("/guest/path/project.csproj", RazorConfiguration.Default, "project");
            ProjectSnapshotManager.ProjectAdded(hostProject);
            ProjectSnapshotManager.ProjectConfigurationChanged(hostProject);
            var args = new ProjectChangeEventProxyArgs(oldHandle, newHandle, ProjectProxyChangeKind.ProjectChanged);

            // Act
            synchronizationService.UpdateGuestProjectManager(args);

            // Assert
            var project = Assert.Single(ProjectSnapshotManager.Projects);
            Assert.Equal("/guest/path/project.csproj", project.FilePath);
            Assert.Same(newConfiguration, project.Configuration);
            Assert.Empty(project.TagHelpers);
        }

        [Fact]
        public void UpdateGuestProjectManager_ProjectChanged_ProjectWorkspaceStateChange()
        {
            // Arrange
            var oldHandle = new ProjectSnapshotHandleProxy(
                new Uri("vsls:/path/project.csproj"),
                RazorConfiguration.Default,
                "project",
                ProjectWorkspaceState.Default);
            var newProjectWorkspaceState = ProjectWorkspaceStateWithTagHelpers;
            var newHandle = new ProjectSnapshotHandleProxy(
                oldHandle.FilePath,
                oldHandle.Configuration,
                oldHandle.RootNamespace,
                newProjectWorkspaceState);
            var synchronizationService = new ProjectSnapshotSynchronizationService(
                JoinableTaskFactory,
                SessionContext,
                Mock.Of<IProjectSnapshotManagerProxy>(),
                ProjectSnapshotManager);
            var hostProject = new HostProject("/guest/path/project.csproj", RazorConfiguration.Default, "project");
            ProjectSnapshotManager.ProjectAdded(hostProject);
            ProjectSnapshotManager.ProjectWorkspaceStateChanged(hostProject.FilePath, oldHandle.ProjectWorkspaceState);
            var args = new ProjectChangeEventProxyArgs(oldHandle, newHandle, ProjectProxyChangeKind.ProjectChanged);

            // Act
            synchronizationService.UpdateGuestProjectManager(args);

            // Assert
            var project = Assert.Single(ProjectSnapshotManager.Projects);
            Assert.Equal("/guest/path/project.csproj", project.FilePath);
            Assert.Same(RazorConfiguration.Default, project.Configuration);
            Assert.Same(newProjectWorkspaceState.TagHelpers, project.TagHelpers);
        }
    }
}
