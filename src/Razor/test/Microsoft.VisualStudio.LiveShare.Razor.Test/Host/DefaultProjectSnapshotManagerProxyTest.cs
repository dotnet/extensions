// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Host
{
    public class DefaultProjectSnapshotManagerProxyTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectSnapshotManagerProxyTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskFactory = new JoinableTaskFactory(joinableTaskContext.Context);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }

        [Fact]
        public async Task CalculateUpdatedStateAsync_ReturnsStateForAllProjects()
        {
            // Arrange
            var tagHelper1 = TagHelperDescriptorBuilder.Create("test1", "TestAssembly1").Build();
            var project1 = new TestProjectSnapshot("/host/path/to/project1.csproj", tagHelper1);
            var tagHelper2 = TagHelperDescriptorBuilder.Create("test2", "TestAssembly2").Build();
            var project2 = new TestProjectSnapshot("/host/path/to/project2.csproj", tagHelper2);
            var projects = new ProjectSnapshot[] { project1, project2 };
            var projectSnapshotManager = new TestProjectSnapshotManager(projects);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var state = await JoinableTaskFactory.RunAsync(() => proxy.CalculateUpdatedStateAsync(projects));

            // Assert
            Assert.Collection(
                state.ProjectHandles,
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project1.csproj", handle.FilePath.ToString());
                    var tagHelper = Assert.Single(handle.TagHelpers);
                    Assert.Equal(tagHelper1, tagHelper);
                },
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project2.csproj", handle.FilePath.ToString());
                    var tagHelper = Assert.Single(handle.TagHelpers);
                    Assert.Equal(tagHelper2, tagHelper);
                });
        }

        [Fact]
        public async Task Changed_TriggersOnSnapshotManagerChanged()
        {
            // Arrange
            var projectSnapshotManager = new TestProjectSnapshotManager(new TestProjectSnapshot("/host/path/to/project1.csproj"));
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);
            var snapshot = new TestProjectSnapshot("/host/path/to/project1.csproj");
            var changedArgs = new ProjectChangeEventArgs(snapshot, snapshot, ProjectChangeKind.ProjectChanged);
            var called = false;
            proxy.Changed += (sender, args) =>
            {
                called = true;
                Assert.Equal($"vsls:/path/to/project1.csproj", args.Change.ProjectFilePath.ToString());
                Assert.Equal(ProjectProxyChangeKind.ProjectChanged, args.Change.Kind);
                var handle = Assert.Single(args.State.ProjectHandles);
                Assert.Equal("vsls:/path/to/project1.csproj", handle.FilePath.ToString());
                Assert.Empty(handle.TagHelpers);
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
            var projectSnapshotManager = new TestProjectSnapshotManager(new TestProjectSnapshot("/host/path/to/project1.csproj"));
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true), 
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);
            var snapshot = new TestProjectSnapshot("/host/path/to/project1.csproj");
            var changedArgs = new ProjectChangeEventArgs(snapshot, snapshot, ProjectChangeKind.ProjectChanged);
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
            var expectedProject = new TestProjectSnapshot("/host/path/to/project1.csproj");
            var projectSnapshotManager = new TestProjectSnapshotManager(expectedProject);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var projects = await proxy.GetLatestProjectsAsync();

            // Assert
            var project = Assert.Single(projects);
            Assert.Same(expectedProject, project);
        }

        [Fact]
        public async Task GetStateAsync_ReturnsProjectState()
        {
            // Arrange
            var tagHelper1 = TagHelperDescriptorBuilder.Create("test1", "TestAssembly1").Build();
            var project1 = new TestProjectSnapshot("/host/path/to/project1.csproj", tagHelper1);
            var tagHelper2 = TagHelperDescriptorBuilder.Create("test2", "TestAssembly2").Build();
            var project2 = new TestProjectSnapshot("/host/path/to/project2.csproj", tagHelper2);
            var projects = new ProjectSnapshot[] { project1, project2 };
            var projectSnapshotManager = new TestProjectSnapshotManager(projects);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var state = await JoinableTaskFactory.RunAsync(() => proxy.GetStateAsync(CancellationToken.None));

            // Assert
            Assert.Collection(
                state.ProjectHandles,
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project1.csproj", handle.FilePath.ToString());
                    var tagHelper = Assert.Single(handle.TagHelpers);
                    Assert.Equal(tagHelper1, tagHelper);
                },
                handle =>
                {
                    Assert.Equal("vsls:/path/to/project2.csproj", handle.FilePath.ToString());
                    var tagHelper = Assert.Single(handle.TagHelpers);
                    Assert.Equal(tagHelper2, tagHelper);
                });
        }

        [Fact]
        public async Task GetStateAsync_CachesState()
        {
            // Arrange
            var project = new TestProjectSnapshot("/host/path/to/project2.csproj");
            var projectSnapshotManager = new TestProjectSnapshotManager(project);
            var proxy = new DefaultProjectSnapshotManagerProxy(
                new TestCollaborationSession(true),
                Dispatcher,
                projectSnapshotManager,
                JoinableTaskFactory);

            // Act
            var state1 = await JoinableTaskFactory.RunAsync(() => proxy.GetStateAsync(CancellationToken.None));
            var state2 = await JoinableTaskFactory.RunAsync(() => proxy.GetStateAsync(CancellationToken.None));

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
