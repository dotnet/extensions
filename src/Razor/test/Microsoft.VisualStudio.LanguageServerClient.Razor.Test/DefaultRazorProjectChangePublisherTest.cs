// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.OperationProgress;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Test
{
    public class TestServiceProvider : IServiceProvider
    {
        public TestServiceProvider()
        {
        }

        public object GetService(Type serviceType)
        {
            return new TestVsOperationProgressStatusService();
        }

        private class TestVsOperationProgressStatusService : IVsOperationProgressStatusService
        {

            public TestVsOperationProgressStatusService()
            {
            }

            public IVsOperationProgressStageStatus GetStageStatus(string operationProgressStageId)
            {
                throw new NotImplementedException();
            }

            public IVsOperationProgressStageStatusForSolutionLoad GetStageStatusForSolutionLoad(string operationProgressStageId)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class DefaultRazorProjectChangePublisherTest : LanguageServerTestBase
    {
        private readonly RazorLogger RazorLogger = Mock.Of<RazorLogger>(MockBehavior.Strict);

        public DefaultRazorProjectChangePublisherTest()
        {
            ProjectSnapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
        }

        private ProjectSnapshotManagerBase ProjectSnapshotManager { get; }

        private ProjectConfigurationFilePathStore ProjectConfigurationFilePathStore { get; } = new DefaultProjectConfigurationFilePathStore();

        [Fact]
        public void ProjectManager_Changed_NotActive_Noops()
        {
            // Arrange
            var attemptedToSerialize = false;
            var hostProject = new HostProject("/path/to/project.csproj", RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
            var hostDocument = new HostDocument("/path/to/file.razor", "file.razor");
            ProjectSnapshotManager.ProjectAdded(hostProject);
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) => attemptedToSerialize = true)
            {
                EnqueueDelay = 10,
            };
            publisher.Initialize(ProjectSnapshotManager);

            // Act
            ProjectSnapshotManager.DocumentAdded(hostProject, hostDocument, new EmptyTextLoader(hostDocument.FilePath));

            // Assert
            Assert.Empty(publisher._deferredPublishTasks);
            Assert.False(attemptedToSerialize);
        }

        [Fact]
        public void ProjectManager_Changed_DocumentOpened_UninitializedProject_NotActive_Noops()
        {
            // Arrange
            var attemptedToSerialize = false;
            var hostProject = new HostProject("/path/to/project.csproj", RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
            var hostDocument = new HostDocument("/path/to/file.razor", "file.razor");
            ProjectSnapshotManager.ProjectAdded(hostProject);
            ProjectSnapshotManager.DocumentAdded(hostProject, hostDocument, new EmptyTextLoader(hostDocument.FilePath));
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) => attemptedToSerialize = true)
            {
                EnqueueDelay = 10,
            };
            publisher.Initialize(ProjectSnapshotManager);

            // Act
            ProjectSnapshotManager.DocumentOpened(hostProject.FilePath, hostDocument.FilePath, SourceText.From(string.Empty));

            // Assert
            Assert.Empty(publisher._deferredPublishTasks);
            Assert.False(attemptedToSerialize);
        }

        [Fact]
        public void ProjectManager_Changed_DocumentOpened_InitializedProject_NotActive_Publishes()
        {
            // Arrange
            var serializationSuccessful = false;
            var hostProject = new HostProject("/path/to/project.csproj", RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
            var hostDocument = new HostDocument("/path/to/file.razor", "file.razor");
            ProjectSnapshotManager.ProjectAdded(hostProject);
            ProjectSnapshotManager.ProjectWorkspaceStateChanged(hostProject.FilePath, ProjectWorkspaceState.Default);
            ProjectSnapshotManager.DocumentAdded(hostProject, hostDocument, new EmptyTextLoader(hostDocument.FilePath));
            var projectSnapshot = ProjectSnapshotManager.Projects[0];
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            ProjectConfigurationFilePathStore.Set(projectSnapshot.FilePath, expectedConfigurationFilePath);
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
                    serializationSuccessful = true;
                })
            {
                EnqueueDelay = 10,
            };
            publisher.Initialize(ProjectSnapshotManager);

            // Act
            ProjectSnapshotManager.DocumentOpened(hostProject.FilePath, hostDocument.FilePath, SourceText.From(string.Empty));

            // Assert
            Assert.Empty(publisher._deferredPublishTasks);
            Assert.True(serializationSuccessful);
        }

        [Theory]
        [InlineData(ProjectChangeKind.DocumentAdded)]
        [InlineData(ProjectChangeKind.DocumentRemoved)]
        [InlineData(ProjectChangeKind.ProjectChanged)]
        internal async Task ProjectManager_Changed_EnqueuesPublishAsync(ProjectChangeKind changeKind)
        {
            // Arrange
            var serializationSuccessful = false;
            var projectSnapshot = CreateProjectSnapshot("/path/to/project.csproj", new ProjectWorkspaceState(ImmutableArray<TagHelperDescriptor>.Empty, CodeAnalysis.CSharp.LanguageVersion.Default));
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.Same(projectSnapshot, snapshot);
                    Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
                    serializationSuccessful = true;
                })
            {
                EnqueueDelay = 10,
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            ProjectConfigurationFilePathStore.Set(projectSnapshot.FilePath, expectedConfigurationFilePath);
            var args = ProjectChangeEventArgs.CreateTestInstance(projectSnapshot, projectSnapshot, documentFilePath: null, changeKind);

            // Act
            publisher.ProjectSnapshotManager_Changed(null, args);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value.ConfigureAwait(false);
            Assert.True(serializationSuccessful);
        }

        [Fact]
        public async Task ProjectManager_Changed_ProjectRemoved_AfterEnqueuedPublishAsync()
        {
            // Arrange
            var attemptedToSerialize = false;
            var projectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) => attemptedToSerialize = true)
            {
                EnqueueDelay = 10,
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            ProjectConfigurationFilePathStore.Set(projectSnapshot.FilePath, expectedConfigurationFilePath);
            publisher.EnqueuePublish(projectSnapshot);
            var args = ProjectChangeEventArgs.CreateTestInstance(projectSnapshot, newer: null, documentFilePath: null, ProjectChangeKind.ProjectRemoved);

            // Act
            publisher.ProjectSnapshotManager_Changed(null, args);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value.ConfigureAwait(false);

            Assert.False(attemptedToSerialize);
        }

        [Fact]
        public async Task EnqueuePublish_BatchesPublishRequestsAsync()
        {
            // Arrange
            var serializationSuccessful = false;
            var firstSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var secondSnapshot = CreateProjectSnapshot("/path/to/project.csproj", new[] { "/path/to/file.cshtml" });
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.Same(secondSnapshot, snapshot);
                    Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
                    serializationSuccessful = true;
                })
            {
                EnqueueDelay = 10,
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            ProjectConfigurationFilePathStore.Set(firstSnapshot.FilePath, expectedConfigurationFilePath);

            // Act
            publisher.EnqueuePublish(firstSnapshot);
            publisher.EnqueuePublish(secondSnapshot);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value.ConfigureAwait(false);
            Assert.True(serializationSuccessful);
        }

        [Fact]
        public void Publish_UnsetConfigurationFilePath_Noops()
        {
            // Arrange
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger)
            {
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            var omniSharpProjectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");

            // Act & Assert
            publisher.Publish(omniSharpProjectSnapshot);
        }

        [Fact]
        public void Publish_PublishesToSetPublishFilePath()
        {
            // Arrange
            var serializationSuccessful = false;
            var omniSharpProjectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.Same(omniSharpProjectSnapshot, snapshot);
                    Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
                    serializationSuccessful = true;
                })
            {
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            ProjectConfigurationFilePathStore.Set(omniSharpProjectSnapshot.FilePath, expectedConfigurationFilePath);

            // Act
            publisher.Publish(omniSharpProjectSnapshot);

            // Assert
            Assert.True(serializationSuccessful);
        }

        [ForegroundFact]
        public async Task ProjectAdded_PublishesToCorrectFilePathAsync()
        {
            // Arrange
            var serializationSuccessful = false;
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";

            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
                    serializationSuccessful = true;
                })
            {
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            var projectFilePath = "/path/to/project.csproj";
            var hostProject = new HostProject(projectFilePath, RazorConfiguration.Default, "TestRootNamespace");
            ProjectConfigurationFilePathStore.Set(hostProject.FilePath, expectedConfigurationFilePath);
            var projectWorkspaceState = new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), CodeAnalysis.CSharp.LanguageVersion.Default);

            // Act
            await RunOnForegroundAsync(() =>
            {
                ProjectSnapshotManager.ProjectAdded(hostProject);
                ProjectSnapshotManager.ProjectWorkspaceStateChanged(projectFilePath, projectWorkspaceState);
            }).ConfigureAwait(false);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value.ConfigureAwait(false);
            Assert.True(serializationSuccessful);
        }

        [ForegroundFact]
        public async Task ProjectAdded_DoesNotPublishWithoutProjectWorkspaceStateAsync()
        {
            // Arrange
            var serializationSuccessful = false;
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";

            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.True(false, "Serialization should not have been atempted because there is no ProjectWorkspaceState.");
                    serializationSuccessful = true;
                })
            {
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            var hostProject = new HostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
            ProjectConfigurationFilePathStore.Set(hostProject.FilePath, expectedConfigurationFilePath);

            // Act
            await RunOnForegroundAsync(() => ProjectSnapshotManager.ProjectAdded(hostProject)).ConfigureAwait(false);

            Assert.Empty(publisher._deferredPublishTasks);

            // Assert
            Assert.False(serializationSuccessful);
        }

        [ForegroundFact]
        public async Task ProjectRemoved_UnSetPublishFilePath_NoopsAsync()
        {
            // Arrange
            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger)
            {
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            var hostProject = new HostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
            await RunOnForegroundAsync(() => ProjectSnapshotManager.ProjectAdded(hostProject)).ConfigureAwait(false);

            // Act & Assert
            await RunOnForegroundAsync(() => ProjectSnapshotManager.ProjectRemoved(hostProject)).ConfigureAwait(false);

            Assert.Empty(publisher._deferredPublishTasks);
        }

        [ForegroundFact]
        public async Task ProjectAdded_DoesNotFireWhenNotReadyAsync()
        {
            // Arrange
            var serializationSuccessful = false;
            var expectedConfigurationFilePath = "/path/to/obj/bin/Debug/project.razor.json";

            var publisher = new TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore,
                RazorLogger,
                onSerializeToFile: (snapshot, configurationFilePath) =>
                {
                    Assert.Equal(expectedConfigurationFilePath, configurationFilePath);
                    serializationSuccessful = true;
                },
                shouldSerialize: false)
            {
                _active = true,
            };
            publisher.Initialize(ProjectSnapshotManager);
            var projectFilePath = "/path/to/project.csproj";
            var hostProject = new HostProject(projectFilePath, RazorConfiguration.Default, "TestRootNamespace");
            ProjectConfigurationFilePathStore.Set(hostProject.FilePath, expectedConfigurationFilePath);
            var projectWorkspaceState = new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), CodeAnalysis.CSharp.LanguageVersion.Default);

            // Act
            await RunOnForegroundAsync(() =>
            {
                ProjectSnapshotManager.ProjectAdded(hostProject);
                ProjectSnapshotManager.ProjectWorkspaceStateChanged(projectFilePath, projectWorkspaceState);
            }).ConfigureAwait(false);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value.ConfigureAwait(false);
            Assert.False(serializationSuccessful);
        }

        internal ProjectSnapshot CreateProjectSnapshot(string projectFilePath, ProjectWorkspaceState projectWorkspaceState = null)
        {
            var testProjectSnapshot = TestProjectSnapshot.Create(projectFilePath, projectWorkspaceState);

            return testProjectSnapshot;
        }

        internal ProjectSnapshot CreateProjectSnapshot(string projectFilePath, string[] documentFilePaths)
        {
            var testProjectSnapshot = TestProjectSnapshot.Create(projectFilePath, documentFilePaths);

            return testProjectSnapshot;
        }

        internal ProjectSnapshotManagerBase CreateProjectSnapshotManager(bool allowNotifyListeners = false)
        {
            var snapshotManager = TestProjectSnapshotManager.Create(Dispatcher);
            snapshotManager.AllowNotifyListeners = allowNotifyListeners;

            return snapshotManager;
        }

        protected Task RunOnForegroundAsync(Action action)
        {
            return Task.Factory.StartNew(
                () => action(),
                CancellationToken.None,
                TaskCreationOptions.None,
                Dispatcher.ForegroundScheduler);
        }

        protected Task<TReturn> RunOnForegroundAsync<TReturn>(Func<TReturn> action)
        {
            return Task.Factory.StartNew(
                () => action(),
                CancellationToken.None,
                TaskCreationOptions.None,
                Dispatcher.ForegroundScheduler);
        }

        protected Task RunOnForegroundAsync(Func<Task> action)
        {
            return Task.Factory.StartNew(
                async () => await action().ConfigureAwait(true),
                CancellationToken.None,
                TaskCreationOptions.None,
                Dispatcher.ForegroundScheduler);
        }

        private class TestDefaultRazorProjectChangePublisher : DefaultRazorProjectChangePublisher
        {
            private static readonly Mock<LSPEditorFeatureDetector> _lspEditorFeatureDetector = new Mock<LSPEditorFeatureDetector>(MockBehavior.Strict);

            private readonly Action<ProjectSnapshot, string> _onSerializeToFile;

            private readonly bool _shouldSerialize;

            static TestDefaultRazorProjectChangePublisher()
            {
                _lspEditorFeatureDetector
                    .Setup(t => t.IsLSPEditorFeatureEnabled())
                    .Returns(true);
            }

            public TestDefaultRazorProjectChangePublisher(
                ProjectConfigurationFilePathStore projectStatePublishFilePathStore,
                RazorLogger logger,
                Action<ProjectSnapshot, string> onSerializeToFile = null,
                bool shouldSerialize = true)
                : base(_lspEditorFeatureDetector.Object, projectStatePublishFilePathStore, new TestServiceProvider(), logger)
            {
                _onSerializeToFile = onSerializeToFile ?? ((_, __) => throw new XunitException("SerializeToFile should not have been called."));
                _shouldSerialize = shouldSerialize;
            }

            protected override void SerializeToFile(ProjectSnapshot projectSnapshot, string configurationFilePath) => _onSerializeToFile?.Invoke(projectSnapshot, configurationFilePath);

            protected override bool ShouldSerialize(string configurationFilePath)
            {
                return _shouldSerialize;
            }
        }
    }
}
