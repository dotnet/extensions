// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Razor.Test;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class WorkspaceProjectStateChangeDetectorTest : ForegroundDispatcherWorkspaceTestBase
    {
        public WorkspaceProjectStateChangeDetectorTest()
        {
            EmptySolution = Workspace.CurrentSolution.GetIsolatedSolution();

            var projectId1 = ProjectId.CreateNewId("One");
            var projectId2 = ProjectId.CreateNewId("Two");
            var projectId3 = ProjectId.CreateNewId("Three");

            CshtmlDocumentId = DocumentId.CreateNewId(projectId1);
            var cshtmlDocumentInfo = DocumentInfo.Create(CshtmlDocumentId, "Test", filePath: "file.cshtml.g.cs");
            RazorDocumentId = DocumentId.CreateNewId(projectId1);
            var razorDocumentInfo = DocumentInfo.Create(RazorDocumentId, "Test", filePath: "file.razor.g.cs");

            SolutionWithTwoProjects = Workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(
                    projectId1,
                    VersionStamp.Default,
                    "One",
                    "One",
                    LanguageNames.CSharp,
                    filePath: "One.csproj",
                    documents: new[] { cshtmlDocumentInfo, razorDocumentInfo }))
                .AddProject(ProjectInfo.Create(
                    projectId2,
                    VersionStamp.Default,
                    "Two",
                    "Two",
                    LanguageNames.CSharp,
                    filePath: "Two.csproj"));

            SolutionWithOneProject = EmptySolution.GetIsolatedSolution()
                .AddProject(ProjectInfo.Create(
                    projectId3,
                    VersionStamp.Default,
                    "Three",
                    "Three",
                    LanguageNames.CSharp,
                    filePath: "Three.csproj"));

            ProjectNumberOne = SolutionWithTwoProjects.GetProject(projectId1);
            ProjectNumberTwo = SolutionWithTwoProjects.GetProject(projectId2);
            ProjectNumberThree = SolutionWithOneProject.GetProject(projectId3);

            HostProjectOne = new HostProject("One.csproj", FallbackRazorConfiguration.MVC_1_1, "One");
            HostProjectTwo = new HostProject("Two.csproj", FallbackRazorConfiguration.MVC_1_1, "Two");
            HostProjectThree = new HostProject("Three.csproj", FallbackRazorConfiguration.MVC_1_1, "Three");
        }

        private HostProject HostProjectOne { get; }

        private HostProject HostProjectTwo { get; }

        private HostProject HostProjectThree { get; }

        private Solution EmptySolution { get; }

        private Solution SolutionWithOneProject { get; }

        private Solution SolutionWithTwoProjects { get; }

        private Project ProjectNumberOne { get; }

        private Project ProjectNumberTwo { get; }

        private Project ProjectNumberThree { get; }

        public DocumentId CshtmlDocumentId { get; }

        public DocumentId RazorDocumentId { get; }

        [ForegroundTheory]
        [InlineData(WorkspaceChangeKind.SolutionAdded)]
        [InlineData(WorkspaceChangeKind.SolutionChanged)]
        [InlineData(WorkspaceChangeKind.SolutionCleared)]
        [InlineData(WorkspaceChangeKind.SolutionReloaded)]
        [InlineData(WorkspaceChangeKind.SolutionRemoved)]
        public void WorkspaceChanged_SolutionEvents_EnqueuesUpdatesForProjectsInSolution(WorkspaceChangeKind kind)
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectOne);
            projectManager.ProjectAdded(HostProjectTwo);

            var e = new WorkspaceChangeEventArgs(kind, oldSolution: EmptySolution, newSolution: SolutionWithTwoProjects);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            Assert.Collection(
                workspaceStateGenerator.UpdateQueue,
                p => Assert.Equal(ProjectNumberOne.Id, p.workspaceProject.Id),
                p => Assert.Equal(ProjectNumberTwo.Id, p.workspaceProject.Id));
        }

        [ForegroundTheory]
        [InlineData(WorkspaceChangeKind.SolutionAdded)]
        [InlineData(WorkspaceChangeKind.SolutionChanged)]
        [InlineData(WorkspaceChangeKind.SolutionCleared)]
        [InlineData(WorkspaceChangeKind.SolutionReloaded)]
        [InlineData(WorkspaceChangeKind.SolutionRemoved)]
        public void WorkspaceChanged_SolutionEvents_EnqueuesStateClear_EnqueuesSolutionProjectUpdates(WorkspaceChangeKind kind)
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectOne);
            projectManager.ProjectAdded(HostProjectTwo);
            projectManager.ProjectAdded(HostProjectThree);

            // Initialize with a project. This will get removed.
            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.SolutionAdded, oldSolution: EmptySolution, newSolution: SolutionWithOneProject);
            detector.Workspace_WorkspaceChanged(Workspace, e);

            e = new WorkspaceChangeEventArgs(kind, oldSolution: SolutionWithOneProject, newSolution: SolutionWithTwoProjects);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            Assert.Collection(
                workspaceStateGenerator.UpdateQueue,
                p => Assert.Equal(ProjectNumberThree.Id, p.workspaceProject.Id),
                p => Assert.Null(p.workspaceProject),
                p => Assert.Equal(ProjectNumberOne.Id, p.workspaceProject.Id),
                p => Assert.Equal(ProjectNumberTwo.Id, p.workspaceProject.Id));
        }

        [ForegroundTheory]
        [InlineData(WorkspaceChangeKind.ProjectChanged)]
        [InlineData(WorkspaceChangeKind.ProjectReloaded)]
        public async Task WorkspaceChanged_ProjectChangeEvents_UpdatesProjectState_AfterDelay(WorkspaceChangeKind kind)
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator)
            {
                EnqueueDelay = 1,
                BlockDelayedUpdateWorkEnqueue = new ManualResetEventSlim(initialState: false),
            };

            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectOne);

            var solution = SolutionWithTwoProjects.WithProjectAssemblyName(ProjectNumberOne.Id, "Changed");
            var e = new WorkspaceChangeEventArgs(kind, oldSolution: SolutionWithTwoProjects, newSolution: solution, projectId: ProjectNumberOne.Id);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            //
            // The change hasn't come through yet.
            Assert.Empty(workspaceStateGenerator.UpdateQueue);

            detector.BlockDelayedUpdateWorkEnqueue.Set();

            await detector._deferredUpdates.Single().Value;

            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(update.workspaceProject.Id, ProjectNumberOne.Id);
            Assert.Equal(update.projectSnapshot.FilePath, HostProjectOne.FilePath);
        }

        [ForegroundFact]
        public async Task WorkspaceChanged_DocumentChanged_CSHTML_UpdatesProjectState_AfterDelay()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator)
            {
                EnqueueDelay = 1,
                BlockDelayedUpdateWorkEnqueue = new ManualResetEventSlim(initialState: false),
            };

            Workspace.TryApplyChanges(SolutionWithTwoProjects);
            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectOne);
            workspaceStateGenerator.ClearQueue();

            var solution = SolutionWithTwoProjects.WithDocumentText(CshtmlDocumentId, SourceText.From("Hello World"));
            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: SolutionWithTwoProjects, newSolution: solution, projectId: ProjectNumberOne.Id, CshtmlDocumentId);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            //
            // The change hasn't come through yet.
            Assert.Empty(workspaceStateGenerator.UpdateQueue);

            detector.BlockDelayedUpdateWorkEnqueue.Set();

            await detector._deferredUpdates.Single().Value;

            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(update.workspaceProject.Id, ProjectNumberOne.Id);
            Assert.Equal(update.projectSnapshot.FilePath, HostProjectOne.FilePath);
        }

        [ForegroundFact]
        public async Task WorkspaceChanged_DocumentChanged_Razor_UpdatesProjectState_AfterDelay()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator)
            {
                EnqueueDelay = 1,
                BlockDelayedUpdateWorkEnqueue = new ManualResetEventSlim(initialState: false),
            };

            Workspace.TryApplyChanges(SolutionWithTwoProjects);
            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectOne);
            workspaceStateGenerator.ClearQueue();

            var solution = SolutionWithTwoProjects.WithDocumentText(RazorDocumentId, SourceText.From("Hello World"));
            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: SolutionWithTwoProjects, newSolution: solution, projectId: ProjectNumberOne.Id, RazorDocumentId);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            //
            // The change hasn't come through yet.
            Assert.Empty(workspaceStateGenerator.UpdateQueue);

            detector.BlockDelayedUpdateWorkEnqueue.Set();

            await detector._deferredUpdates.Single().Value;

            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(update.workspaceProject.Id, ProjectNumberOne.Id);
            Assert.Equal(update.projectSnapshot.FilePath, HostProjectOne.FilePath);
        }

        [ForegroundFact]
        public void WorkspaceChanged_ProjectRemovedEvent_QueuesProjectStateRemoval()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectOne);
            projectManager.ProjectAdded(HostProjectTwo);

            var solution = SolutionWithTwoProjects.RemoveProject(ProjectNumberOne.Id);
            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.ProjectRemoved, oldSolution: SolutionWithTwoProjects, newSolution: solution, projectId: ProjectNumberOne.Id);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            Assert.Collection(
                workspaceStateGenerator.UpdateQueue,
                p => Assert.Null(p.workspaceProject));
        }

        [ForegroundFact]
        public void WorkspaceChanged_ProjectAddedEvent_AddsProject()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var projectManager = new TestProjectSnapshotManager(new[] { detector }, Workspace);
            projectManager.ProjectAdded(HostProjectThree);

            var solution = SolutionWithOneProject;
            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.ProjectAdded, oldSolution: EmptySolution, newSolution: solution, projectId: ProjectNumberThree.Id);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            Assert.Collection(
                workspaceStateGenerator.UpdateQueue,
                p => Assert.Equal(ProjectNumberThree.Id, p.workspaceProject.Id));
        }

        private class TestProjectSnapshotManager : DefaultProjectSnapshotManager
        {
            public TestProjectSnapshotManager(IEnumerable<ProjectSnapshotChangeTrigger> triggers, Workspace workspace)
                : base(Mock.Of<ForegroundDispatcher>(), Mock.Of<ErrorReporter>(), triggers, workspace)
            {
            }
        }
    }
}
