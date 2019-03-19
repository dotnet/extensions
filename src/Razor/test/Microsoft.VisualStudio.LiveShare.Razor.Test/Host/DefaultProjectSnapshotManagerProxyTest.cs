// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Microsoft.VisualStudio.Threading;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Host
{
    public class DefaultProjectSnapshotManagerProxyTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectSnapshotManagerProxyTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskFactory = new JoinableTaskFactory(joinableTaskContext.Context);
            Workspace = TestWorkspace.Create();
            var projectWorkspaceState1 = new ProjectWorkspaceState(new[]
            {
                TagHelperDescriptorBuilder.Create("test1", "TestAssembly1").Build(),
            }, default);
            ProjectSnapshot1 = new DefaultProjectSnapshot(
                ProjectState.Create(
                    Workspace.Services,
                    new HostProject("/host/path/to/project1.csproj", RazorConfiguration.Default, "project1"),
                    projectWorkspaceState1));
            var projectWorkspaceState2 = new ProjectWorkspaceState(new[]
            {
                TagHelperDescriptorBuilder.Create("test2", "TestAssembly2").Build(),
            }, default);
            ProjectSnapshot2 = new DefaultProjectSnapshot(
                ProjectState.Create(
                    Workspace.Services,
                    new HostProject("/host/path/to/project2.csproj", RazorConfiguration.Default, "project2"),
                    projectWorkspaceState2));
        }

        private JoinableTaskFactory JoinableTaskFactory { get; }

        private Workspace Workspace { get; }

        private ProjectSnapshot ProjectSnapshot1 { get; }

        private ProjectSnapshot ProjectSnapshot2 { get; }

        [Fact]
        public async Task CalculateUpdatedStateAsync_ReturnsStateForAllProjects()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(ProjectSnapshot1, ProjectSnapshot2);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var state = await JoinableTaskFactory.RunAsync(() => proxy.CalculateUpdatedStateAsync(projectSnapshotManager.Projects));

            // Assert
            Assert.Collection(
                state.ProjectHandles,
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project1.csproj", handle.FilePath.ToString());
                    Assert.Equal(ProjectSnapshot1.TagHelpers, handle.ProjectWorkspaceState.TagHelpers);
                },
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project2.csproj", handle.FilePath.ToString());
                    Assert.Equal(ProjectSnapshot2.TagHelpers, handle.ProjectWorkspaceState.TagHelpers);
                });
        }

        [Fact]
        public async Task Changed_TriggersOnSnapshotManagerChanged()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(ProjectSnapshot1);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);
            var changedArgs = new ProjectChangeEventArgs(ProjectSnapshot1, ProjectSnapshot1, ProjectChangeKind.ProjectChanged);
            var called = false;
            proxy.Changed += (sender, args) =>
            {
                called = true;
                Assert.Equal($"vsls:/path/to/project1.csproj", args.ProjectFilePath.ToString());
                Assert.Equal(ProjectProxyChangeKind.ProjectChanged, args.Kind);
                Assert.Equal("vsls:/path/to/project1.csproj", args.Newer.FilePath.ToString());
            };

            // Act
            projectSnapshotManager.TriggerChanged(changedArgs);
            await proxy._processingChangedEventTestTask.JoinAsync();

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Changed_NoopsIfProxyDisposed()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(ProjectSnapshot1);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);
            var changedArgs = new ProjectChangeEventArgs(ProjectSnapshot1, ProjectSnapshot1, ProjectChangeKind.ProjectChanged);
            proxy.Changed += (sender, args) => throw new InvalidOperationException("Should not have been called.");
            proxy.Dispose();

            // Act
            projectSnapshotManager.TriggerChanged(changedArgs);

            // Assert
            Assert.Null(proxy._processingChangedEventTestTask);
        }

        [Fact]
        public async Task GetLatestProjectsAsync_ReturnsSnapshotManagerProjects()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(ProjectSnapshot1);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var projects = await proxy.GetLatestProjectsAsync();

            // Assert
            var project = Assert.Single(projects);
            Assert.Same(ProjectSnapshot1, project);
        }

        [Fact]
        public async Task GetStateAsync_ReturnsProjectState()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(ProjectSnapshot1, ProjectSnapshot2);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var state = await JoinableTaskFactory.RunAsync(() => proxy.GetProjectManagerStateAsync(CancellationToken.None));

            // Assert
            Assert.Collection(
                state.ProjectHandles,
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project1.csproj", handle.FilePath.ToString());
                    Assert.Equal(ProjectSnapshot1.TagHelpers, handle.ProjectWorkspaceState.TagHelpers);
                },
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project2.csproj", handle.FilePath.ToString());
                    Assert.Equal(ProjectSnapshot2.TagHelpers, handle.ProjectWorkspaceState.TagHelpers);
                });
        }

        [Fact]
        public async Task GetStateAsync_CachesState()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(ProjectSnapshot1);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var state1 = await JoinableTaskFactory.RunAsync(() => proxy.GetProjectManagerStateAsync(CancellationToken.None));
            var state2 = await JoinableTaskFactory.RunAsync(() => proxy.GetProjectManagerStateAsync(CancellationToken.None));

            // Assert
            Assert.Same(state1, state2);
        }

        private class TestProjectSnapshotManager : ProjectSnapshotManager
        {
            public TestProjectSnapshotManager(params ProjectSnapshot[] projects)
            {
                Projects = projects;
            }

            public override IReadOnlyList<ProjectSnapshot> Projects { get; }

            public override event EventHandler<ProjectChangeEventArgs> Changed;

            public void TriggerChanged(ProjectChangeEventArgs args)
            {
                Changed?.Invoke(this, args);
            }

            public override ProjectSnapshot GetLoadedProject(string filePath)
            {
                throw new NotImplementedException();
            }

            public override ProjectSnapshot GetOrCreateProject(string filePath)
            {
                throw new NotImplementedException();
            }

            public override bool IsDocumentOpen(string documentFilePath)
            {
                throw new NotImplementedException();
            }
        }
    }
}
