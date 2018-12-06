// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class DefaultProjectSnapshotHandleStoreTest : ForegroundDispatcherTestBase
    {
        public DefaultProjectSnapshotHandleStoreTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskFactory = new JoinableTaskFactory(joinableTaskContext.Context);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }

        [Fact]
        public void GetProjectHandles_ReturnsCurrentProjects()
        {
            // Arrange
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, Mock.Of<ProxyAccessor>());
            snapshotStore.UpdateProjects(
                new ProjectSnapshotManagerProxyState(new[]
                {
                    TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/other/project.csproj")),
                    TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")),
                }));

            // Act
            var projects = snapshotStore.GetProjectHandles();

            // Assert
            Assert.Equal(2, projects.Count);
        }

        [Fact]
        public void GetProjectHandles_ReturnsEmptyArrayIfNotInitialized()
        {
            // Arrange
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, Mock.Of<ProxyAccessor>());

            // Act
            var projects = snapshotStore.GetProjectHandles();

            // Assert
            Assert.Empty(projects);
        }

        [Fact]
        public void RemoteProxyStateManager_UpdatesStateAndRaisesChangedEvent()
        {
            // Arrange
            var expectedChangeEventArgs = new ProjectProxyChangeEventArgs(new Uri("vsls:/some/path/project.csproj"), ProjectProxyChangeKind.ProjectChanged);
            var expectedProjectManagerState = new ProjectSnapshotManagerProxyState(new[] { TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")) });
            var setupArgs = new ProjectManagerProxyChangeEventArgs(expectedChangeEventArgs, expectedProjectManagerState);
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, Mock.Of<ProxyAccessor>());
            var called = false;
            snapshotStore.Changed += (sender, args) =>
            {
                called = true;
                Assert.Same(expectedChangeEventArgs, args);
            };

            // Act
            snapshotStore.HostProxyStateManager_Changed(null, setupArgs);

            // Assert
            Assert.True(called);
            Assert.Single(snapshotStore.GetProjectHandles());
        }

        [Fact]
        public async Task InitializeProjects_RaisesChangeEventForEachProjectIfGetProjectHandlesWasCalledBeforeInitialization()
        {
            // Arrange
            var proxy = new Mock<IProjectSnapshotManagerProxy>();
            proxy.Setup(p => p.GetStateAsync(CancellationToken.None))
                .Returns(Task.FromResult(new ProjectSnapshotManagerProxyState(new[]
                {
                    TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")),
                    TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/other/project.csproj")),
                })));
            var accessor = Mock.Of<ProxyAccessor>(a => a.GetProjectSnapshotManagerProxy() == proxy.Object);
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, accessor);
            snapshotStore.GetProjectHandles();
            var calledWith = new List<ProjectProxyChangeEventArgs>();
            snapshotStore.Changed += (sender, args) => calledWith.Add(args);

            // Act
            snapshotStore.InitializeProjects();
            await snapshotStore.TestInitializationTask;

            // Assert
            Assert.Collection(calledWith,
                args =>
                {
                    Assert.Equal(ProjectProxyChangeKind.ProjectAdded, args.Kind);
                    Assert.Equal("vsls:/some/path/project.csproj", args.ProjectFilePath.ToString());
                },
                args =>
                {
                    Assert.Equal(ProjectProxyChangeKind.ProjectAdded, args.Kind);
                    Assert.Equal("vsls:/some/other/project.csproj", args.ProjectFilePath.ToString());
                });
        }

        [Fact]
        public async Task InitializeProjects_UpdatesProjectStateWithoutTriggeringChange()
        {
            // Arrange
            var proxy = new Mock<IProjectSnapshotManagerProxy>();
            proxy.Setup(p => p.GetStateAsync(CancellationToken.None))
                .Returns(Task.FromResult(new ProjectSnapshotManagerProxyState(new[] { TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")) })));
            var accessor = Mock.Of<ProxyAccessor>(a => a.GetProjectSnapshotManagerProxy() == proxy.Object);
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, accessor);
            snapshotStore.Changed += (sender, args) => throw new InvalidOperationException("This should not be called.");

            // Act
            snapshotStore.InitializeProjects();
            await snapshotStore.TestInitializationTask;

            // Assert
            Assert.Single(snapshotStore.GetProjectHandles());
        }

        [Fact]
        public async Task InitializeProjects_DoesNotUpdateProjectsIfInitialized()
        {
            // Arrange
            var proxy = new Mock<IProjectSnapshotManagerProxy>();
            proxy.Setup(p => p.GetStateAsync(CancellationToken.None))
                .Returns(Task.FromResult(new ProjectSnapshotManagerProxyState(Array.Empty<ProjectSnapshotHandleProxy>())));
            var accessor = Mock.Of<ProxyAccessor>(a => a.GetProjectSnapshotManagerProxy() == proxy.Object);
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, accessor);

            // Initialize the initial project state to a project list. This is pretending to be an update event from the proxy
            // that has newer information than initialization.
            snapshotStore.UpdateProjects(new ProjectSnapshotManagerProxyState(new[] { TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj")) }));

            // Act
            snapshotStore.InitializeProjects();
            await snapshotStore.TestInitializationTask;

            // Assert
            Assert.Single(snapshotStore.GetProjectHandles());
        }

        [Fact]
        public void UpdateProjects_UpdatesProjectCollection()
        {
            // Arrange
            var snapshotStore = new DefaultProjectSnapshotHandleStore(Dispatcher, JoinableTaskFactory, Mock.Of<ProxyAccessor>());
            var expectedProject = TestProjectSnapshotHandleProxy.Create(new Uri("vsls:/some/path/project.csproj"));
            var remoteProjectSnapshotManagerState = new ProjectSnapshotManagerProxyState(new List<ProjectSnapshotHandleProxy>() { expectedProject });

            // Act
            snapshotStore.UpdateProjects(remoteProjectSnapshotManagerState);

            // Assert
            var project = Assert.Single(snapshotStore.GetProjectHandles());
            Assert.Same(expectedProject, project);
        }
    }
}
