// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class GuestProjectSnapshotManagerTest : ForegroundDispatcherTestBase
    {
        public Workspace Workspace { get; } = TestWorkspace.Create();

        [Fact]
        public void Projects_ReturnsCurrentProjects()
        {
            // Arrange
            var snapshotStore = CreateSnapshotStore(
                TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/other/project.csproj")),
                TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")));
            var snapshotManager = CreateSnapshotManager(snapshotStore, Workspace);

            // Act & Assert
            Assert.Equal(2, snapshotManager.Projects.Count);
        }

        [Fact]
        public void GetLoadedProject_ReturnsNullIfCanNotFindProject()
        {
            // Arrange
            var snapshotStore = CreateSnapshotStore(
                TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/other/project.csproj")),
                TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")));
            var snapshotManager = CreateSnapshotManager(snapshotStore, Workspace);

            // Act
            var project = snapshotManager.GetLoadedProject("/some/random/path/project.csproj");

            // Assert
            Assert.Null(project);
        }

        [Fact]
        public void GetLoadedProject_ReturnsExistingProject()
        {
            // Arrange
            var expectedProject = TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj"));
            var snapshotStore = CreateSnapshotStore(
                TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/other/project.csproj")),
                expectedProject);
            var snapshotManager = CreateSnapshotManager(snapshotStore, Workspace);

            // Act
            var project = snapshotManager.GetLoadedProject(expectedProject.FilePath.ToString());

            // Assert
            Assert.IsType<TestProjectSnapshot>(project);
            Assert.Equal(expectedProject.FilePath.ToString(), project.FilePath);
        }

        [Fact]
        public void GetOrCreateProject_CreatesNewProject()
        {
            // Arrange
            var snapshotStore = CreateSnapshotStore(TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")));
            var workspace = TestWorkspace.Create();
            var snapshotManager = CreateSnapshotManager(snapshotStore, workspace);
            var expectedNewProjectPath = "/some/random/path/project.csproj";

            // Act
            var project = snapshotManager.GetOrCreateProject(expectedNewProjectPath);

            // Assert
            Assert.IsType<EphemeralProjectSnapshot>(project);
            Assert.Equal(expectedNewProjectPath, project.FilePath);
        }

        [Fact]
        public void GetOrCreateProject_ReturnsExistingProject()
        {
            // Arrange
            var expectedProject = TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj"));
            var snapshotStore = CreateSnapshotStore(
                TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/other/project.csproj")),
                expectedProject);
            var snapshotManager = CreateSnapshotManager(snapshotStore, Workspace);

            // Act
            var project = snapshotManager.GetOrCreateProject(expectedProject.FilePath.ToString());

            // Assert
            Assert.IsType<TestProjectSnapshot>(project);
            Assert.Equal(expectedProject.FilePath.ToString(), project.FilePath);
        }

        [Fact]
        public void ProjectSnapshotStore_Changed_UpdatesStateAndRaisesChangedEvent()
        {
            // Arrange
            var expectedFilePath = new Uri("vsls:/some/path/project.csproj");
            var storeArgs = new ProjectProxyChangeEventArgs(expectedFilePath, ProjectProxyChangeKind.ProjectChanged);
            var snapshotStore = new TestSnapshotStore();
            snapshotStore.ProjectHandles.Add(TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")));
            var snapshotManager = CreateSnapshotManager(snapshotStore, Workspace);
            var initialProjects = snapshotManager.Projects;
            snapshotStore.ProjectHandles.Clear();
            var called = false;
            snapshotManager.Changed += (sender, args) =>
            {
                called = true;
                Assert.Equal(expectedFilePath.ToString(), args.ProjectFilePath);
                Assert.Equal(ProjectChangeKind.ProjectChanged, args.Kind);
            };

            // Act
            snapshotManager.ProjectSnapshotStore_Changed(null, storeArgs);

            // Assert
            Assert.True(called);
            Assert.NotEqual(snapshotManager.Projects.Count, initialProjects.Count);
        }

        private static ProjectSnapshotHandleStore CreateSnapshotStore(params ProjectSnapshotHandleProxy[] snapshotProxies)
        {
            var snapshotStore = new TestSnapshotStore();
            snapshotStore.ProjectHandles.AddRange(snapshotProxies);
            return snapshotStore;
        }

        private GuestProjectSnapshotManager CreateSnapshotManager(ProjectSnapshotHandleStore projectSnapshotHandleStore, Workspace workspace)
        {
            return new TestProjectSnapshotManager(Dispatcher, workspace.Services, projectSnapshotHandleStore, workspace);
        }

        private class TestSnapshotStore : ProjectSnapshotHandleStore
        {
            public override event EventHandler<ProjectProxyChangeEventArgs> Changed
            {
                add { }
                remove { }
            }

            public List<ProjectSnapshotHandleProxy> ProjectHandles { get; } = new List<ProjectSnapshotHandleProxy>();

            public override IReadOnlyList<ProjectSnapshotHandleProxy> GetProjectHandles() => ProjectHandles;
        }

        private class TestProjectSnapshotManager : GuestProjectSnapshotManager
        {
            internal TestProjectSnapshotManager(ForegroundDispatcher foregroundDispatcher, HostWorkspaceServices services, ProjectSnapshotHandleStore projectSnapshotHandleStore, Workspace workspace)
                : base(foregroundDispatcher, services, projectSnapshotHandleStore, new TestProjectSnapshotFactory(), workspace)
            {
            }

            internal override string ResolveGuestPath(Uri filePath)
            {
                return filePath.ToString();
            }
        }

        private class TestProjectSnapshotFactory : ProjectSnapshotFactory
        {
            public override ProjectSnapshot Create(ProjectSnapshotHandleProxy projectHandle) => new TestProjectSnapshot(projectHandle.FilePath.ToString());
        }
    }
}
