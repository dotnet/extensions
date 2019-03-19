// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultProjectSnapshotManagerTest : ForegroundDispatcherWorkspaceTestBase
    {
        public DefaultProjectSnapshotManagerTest()
        {
            TagHelperResolver = new TestTagHelperResolver();

            Documents = new HostDocument[]
            {
                TestProjectData.SomeProjectFile1,
                TestProjectData.SomeProjectFile2,

                // linked file
                TestProjectData.AnotherProjectNestedFile3,

                TestProjectData.SomeProjectComponentFile1,
                TestProjectData.SomeProjectComponentFile2,
            };

            HostProject = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_2_0, TestProjectData.SomeProject.RootNamespace);
            HostProjectWithConfigurationChange = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_1_0, TestProjectData.SomeProject.RootNamespace);

            ProjectManager = new TestProjectSnapshotManager(Dispatcher, Enumerable.Empty<ProjectSnapshotChangeTrigger>(), Workspace);

            ProjectWorkspaceStateWithTagHelpers = new ProjectWorkspaceState(TagHelperResolver.TagHelpers, default);

            SourceText = SourceText.From("Hello world");
        }

        private HostDocument[] Documents { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private ProjectWorkspaceState ProjectWorkspaceStateWithTagHelpers { get; }

        private TestTagHelperResolver TagHelperResolver { get; }

        private TestProjectSnapshotManager ProjectManager { get; }

        private SourceText SourceText { get; }

        protected override void ConfigureWorkspaceServices(List<IWorkspaceService> services)
        {
            services.Add(TagHelperResolver);
        }

        [ForegroundFact]
        public void DocumentAdded_AddsDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Collection(snapshot.DocumentFilePaths.OrderBy(f => f), d => Assert.Equal(Documents[0].FilePath, d));

            Assert.Equal(ProjectChangeKind.DocumentAdded, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void DocumentAdded_AddsDocument_Legacy()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Collection(
                snapshot.DocumentFilePaths.OrderBy(f => f),
                d =>
                {
                    Assert.Equal(Documents[0].FilePath, d);
                    Assert.Equal(FileKinds.Legacy, snapshot.GetDocument(d).FileKind);
                });

            Assert.Equal(ProjectChangeKind.DocumentAdded, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void DocumentAdded_AddsDocument_Component()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[3], null);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Collection(
                snapshot.DocumentFilePaths.OrderBy(f => f),
                d =>
                {
                    Assert.Equal(Documents[3].FilePath, d);
                    Assert.Equal(FileKinds.Component, snapshot.GetDocument(d).FileKind);
                });

            Assert.Equal(ProjectChangeKind.DocumentAdded, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void DocumentAdded_IgnoresDuplicate()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Collection(snapshot.DocumentFilePaths.OrderBy(f => f), d => Assert.Equal(Documents[0].FilePath, d));

            Assert.Null(ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void DocumentAdded_IgnoresUnknownProject()
        {
            // Arrange

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Null(snapshot);
        }

        [ForegroundFact]
        public async Task DocumentAdded_NullLoader_HasEmptyText()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var document = snapshot.GetDocument(snapshot.DocumentFilePaths.Single());

            var text = await document.GetTextAsync();
            Assert.Equal(0, text.Length);
        }

        [ForegroundFact]
        public async Task DocumentAdded_WithLoader_LoadesText()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            var expected = SourceText.From("Hello");

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], TextLoader.From(TextAndVersion.Create(expected, VersionStamp.Default)));

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var document = snapshot.GetDocument(snapshot.DocumentFilePaths.Single());

            var actual = await document.GetTextAsync();
            Assert.Same(expected, actual);
        }

        [ForegroundFact]
        public void DocumentAdded_CachesTagHelpers()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceStateWithTagHelpers);
            ProjectManager.Reset();

            var originalTagHelpers = ProjectManager.GetSnapshot(HostProject).TagHelpers;

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            var newTagHelpers = ProjectManager.GetSnapshot(HostProject).TagHelpers;
            Assert.Same(originalTagHelpers, newTagHelpers);
        }

        [ForegroundFact]
        public void DocumentAdded_CachesProjectEngine()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var projectEngine = snapshot.GetProjectEngine();

            // Act
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);

            // Assert
            snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Same(projectEngine, snapshot.GetProjectEngine());
        }

        [ForegroundFact]
        public void DocumentRemoved_RemovesDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.DocumentAdded(HostProject, Documents[1], null);
            ProjectManager.DocumentAdded(HostProject, Documents[2], null);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentRemoved(HostProject, Documents[1]);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Collection(
                snapshot.DocumentFilePaths.OrderBy(f => f),
                d => Assert.Equal(Documents[2].FilePath, d),
                d => Assert.Equal(Documents[0].FilePath, d));

            Assert.Equal(ProjectChangeKind.DocumentRemoved, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void DocumentRemoved_IgnoresNotFoundDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentRemoved(HostProject, Documents[0]);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Empty(snapshot.DocumentFilePaths);

            Assert.Null(ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void DocumentRemoved_IgnoresUnknownProject()
        {
            // Arrange

            // Act
            ProjectManager.DocumentRemoved(HostProject, Documents[0]);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Null(snapshot);
        }

        [ForegroundFact]
        public void DocumentRemoved_CachesTagHelpers()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceStateWithTagHelpers);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.DocumentAdded(HostProject, Documents[1], null);
            ProjectManager.DocumentAdded(HostProject, Documents[2], null);
            ProjectManager.Reset();

            var originalTagHelpers = ProjectManager.GetSnapshot(HostProject).TagHelpers;

            // Act
            ProjectManager.DocumentRemoved(HostProject, Documents[1]);

            // Assert
            var newTagHelpers = ProjectManager.GetSnapshot(HostProject).TagHelpers;
            Assert.Same(originalTagHelpers, newTagHelpers);
        }

        [ForegroundFact]
        public void DocumentRemoved_CachesProjectEngine()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.DocumentAdded(HostProject, Documents[1], null);
            ProjectManager.DocumentAdded(HostProject, Documents[2], null);
            ProjectManager.Reset();

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var projectEngine = snapshot.GetProjectEngine();

            // Act
            ProjectManager.DocumentRemoved(HostProject, Documents[1]);

            // Assert
            snapshot = ProjectManager.GetSnapshot(HostProject);
            Assert.Same(projectEngine, snapshot.GetProjectEngine());
        }
       [ForegroundFact]
        public async Task DocumentOpened_UpdatesDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.Reset();

            // Act
            ProjectManager.DocumentOpened(HostProject.FilePath, Documents[0].FilePath, SourceText);

            // Assert
            Assert.Equal(ProjectChangeKind.DocumentChanged, ProjectManager.ListenersNotifiedOf);

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var text = await snapshot.GetDocument(Documents[0].FilePath).GetTextAsync();
            Assert.Same(SourceText, text);

            Assert.True(ProjectManager.IsDocumentOpen(Documents[0].FilePath));
        }

        [ForegroundFact]
        public async Task DocumentClosed_UpdatesDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.DocumentOpened(HostProject.FilePath, Documents[0].FilePath, SourceText);
            ProjectManager.Reset();

            var expected = SourceText.From("Hi");
            var textAndVersion = TextAndVersion.Create(expected, VersionStamp.Create());

            Assert.True(ProjectManager.IsDocumentOpen(Documents[0].FilePath));

            // Act
            ProjectManager.DocumentClosed(HostProject.FilePath, Documents[0].FilePath, TextLoader.From(textAndVersion));

            // Assert
            Assert.Equal(ProjectChangeKind.DocumentChanged, ProjectManager.ListenersNotifiedOf);

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var text = await snapshot.GetDocument(Documents[0].FilePath).GetTextAsync();
            Assert.Same(expected, text);
            Assert.False(ProjectManager.IsDocumentOpen(Documents[0].FilePath));
        }


        [ForegroundFact]
        public async Task DocumentClosed_AcceptsChange()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.Reset();

            var expected = SourceText.From("Hi");
            var textAndVersion = TextAndVersion.Create(expected, VersionStamp.Create());

            // Act
            ProjectManager.DocumentClosed(HostProject.FilePath, Documents[0].FilePath, TextLoader.From(textAndVersion));

            // Assert
            Assert.Equal(ProjectChangeKind.DocumentChanged, ProjectManager.ListenersNotifiedOf);

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var text = await snapshot.GetDocument(Documents[0].FilePath).GetTextAsync();
            Assert.Same(expected, text);
        }

        [ForegroundFact]
        public async Task DocumentChanged_Snapshot_UpdatesDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.DocumentOpened(HostProject.FilePath, Documents[0].FilePath, SourceText);
            ProjectManager.Reset();

            var expected = SourceText.From("Hi");

            // Act
            ProjectManager.DocumentChanged(HostProject.FilePath, Documents[0].FilePath, expected);

            // Assert
            Assert.Equal(ProjectChangeKind.DocumentChanged, ProjectManager.ListenersNotifiedOf);

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var text = await snapshot.GetDocument(Documents[0].FilePath).GetTextAsync();
            Assert.Same(expected, text);
        }

        [ForegroundFact]
        public async Task DocumentChanged_Loader_UpdatesDocument()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.DocumentAdded(HostProject, Documents[0], null);
            ProjectManager.DocumentOpened(HostProject.FilePath, Documents[0].FilePath, SourceText);
            ProjectManager.Reset();

            var expected = SourceText.From("Hi");
            var textAndVersion = TextAndVersion.Create(expected, VersionStamp.Create());

            // Act
            ProjectManager.DocumentChanged(HostProject.FilePath, Documents[0].FilePath, TextLoader.From(textAndVersion));

            // Assert
            Assert.Equal(ProjectChangeKind.DocumentChanged, ProjectManager.ListenersNotifiedOf);

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var text = await snapshot.GetDocument(Documents[0].FilePath).GetTextAsync();
            Assert.Same(expected, text);
        }

        [ForegroundFact]
        public void ProjectAdded_WithoutWorkspaceProject_NotifiesListeners()
        {
            // Arrange

            // Act
            ProjectManager.ProjectAdded(HostProject);

            // Assert
            Assert.Equal(ProjectChangeKind.ProjectAdded, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void ProjectConfigurationChanged_ConfigurationChange_ProjectWorkspaceState_NotifiesListeners()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectConfigurationChanged(HostProjectWithConfigurationChange);

            // Assert
            var snapshot = ProjectManager.GetSnapshot(HostProjectWithConfigurationChange);
            Assert.Equal(ProjectChangeKind.ProjectChanged, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void ProjectConfigurationChanged_ConfigurationChange_WithProjectWorkspaceState_NotifiesListeners()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceStateWithTagHelpers);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectConfigurationChanged(HostProjectWithConfigurationChange);

            // Assert
            Assert.Equal(ProjectChangeKind.ProjectChanged, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void ProjectConfigurationChanged_ConfigurationChange_DoesNotCacheProjectEngine()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            var snapshot = ProjectManager.GetSnapshot(HostProject);
            var projectEngine = snapshot.GetProjectEngine();

            // Act
            ProjectManager.ProjectConfigurationChanged(HostProjectWithConfigurationChange);

            // Assert
            snapshot = ProjectManager.GetSnapshot(HostProjectWithConfigurationChange);
            Assert.NotSame(projectEngine, snapshot.GetProjectEngine());
        }

        [ForegroundFact]
        public void ProjectConfigurationChanged_IgnoresUnknownProject()
        {
            // Arrange

            // Act
            ProjectManager.ProjectConfigurationChanged(HostProject);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.Null(ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void ProjectRemoved_RemovesProject_NotifiesListeners()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectRemoved(HostProject);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.Equal(ProjectChangeKind.ProjectRemoved, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void ProjectWorkspaceStateChanged_WithoutHostProject_IgnoresWorkspaceState()
        {
            // Arrange

            // Act
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceStateWithTagHelpers);

            // Assert
            Assert.Empty(ProjectManager.Projects);

            Assert.Null(ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void ProjectWorkspaceStateChanged_WithHostProject_FirstTime_NotifiesListenters()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceStateWithTagHelpers);

            // Assert
            Assert.Equal(ProjectChangeKind.ProjectChanged, ProjectManager.ListenersNotifiedOf);
        }

        [ForegroundFact]
        public void WorkspaceProjectChanged_WithHostProject_NotifiesListenters()
        {
            // Arrange
            ProjectManager.ProjectAdded(HostProject);
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceState.Default);
            ProjectManager.Reset();

            // Act
            ProjectManager.ProjectWorkspaceStateChanged(HostProject.FilePath, ProjectWorkspaceStateWithTagHelpers);

            // Assert
            Assert.Equal(ProjectChangeKind.ProjectChanged, ProjectManager.ListenersNotifiedOf);
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(ForegroundDispatcher dispatcher, IEnumerable<ProjectSnapshotChangeTrigger> triggers, Workspace workspace)
                : base(dispatcher, Mock.Of<ErrorReporter>(), triggers, workspace)
            {
            }

            public ProjectChangeKind? ListenersNotifiedOf { get; private set; }

            public DefaultProjectSnapshot GetSnapshot(HostProject hostProject)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.FilePath == hostProject.FilePath);
            }

            public DefaultProjectSnapshot GetSnapshot(Project workspaceProject)
            {
                return Projects.Cast<DefaultProjectSnapshot>().FirstOrDefault(s => s.FilePath == workspaceProject.FilePath);
            }

            public void Reset()
            {
                ListenersNotifiedOf = null;
            }

            protected override void NotifyListeners(ProjectChangeEventArgs e)
            {
                ListenersNotifiedOf = e.Kind;
            }
        }
    }
}
