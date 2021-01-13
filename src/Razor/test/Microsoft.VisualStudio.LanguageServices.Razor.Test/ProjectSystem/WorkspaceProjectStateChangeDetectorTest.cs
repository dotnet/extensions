// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis.CSharp;
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
            BackgroundVirtualCSharpDocumentId = DocumentId.CreateNewId(projectId1);
            var backgroundDocumentInfo = DocumentInfo.Create(BackgroundVirtualCSharpDocumentId, "Test", filePath: "file.razor__bg__virtual.cs");
            PartialComponentClassDocumentId = DocumentId.CreateNewId(projectId1);
            var partialComponentClassDocumentInfo = DocumentInfo.Create(PartialComponentClassDocumentId, "Test", filePath: "file.razor.cs");

            SolutionWithTwoProjects = Workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(
                    projectId1,
                    VersionStamp.Default,
                    "One",
                    "One",
                    LanguageNames.CSharp,
                    filePath: "One.csproj",
                    documents: new[] { cshtmlDocumentInfo, razorDocumentInfo, partialComponentClassDocumentInfo, backgroundDocumentInfo }))
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

        public DocumentId BackgroundVirtualCSharpDocumentId { get; }

        public DocumentId PartialComponentClassDocumentId { get; }

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

            await detector._deferredUpdates.Single().Value.Task;

            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(update.workspaceProject.Id, ProjectNumberOne.Id);
            Assert.Equal(update.projectSnapshot.FilePath, HostProjectOne.FilePath);
        }

        [ForegroundFact]
        public async Task WorkspaceChanged_DocumentChanged_BackgroundVirtualCS_UpdatesProjectState_AfterDelay()
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

            var solution = SolutionWithTwoProjects.WithDocumentText(BackgroundVirtualCSharpDocumentId, SourceText.From("public class Foo{}"));
            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: SolutionWithTwoProjects, newSolution: solution, projectId: ProjectNumberOne.Id, BackgroundVirtualCSharpDocumentId);

            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            //
            // The change hasn't come through yet.
            Assert.Empty(workspaceStateGenerator.UpdateQueue);

            detector.BlockDelayedUpdateWorkEnqueue.Set();

            await detector._deferredUpdates.Single().Value.Task;

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

            await detector._deferredUpdates.Single().Value.Task;

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

            await detector._deferredUpdates.Single().Value.Task;

            var update = Assert.Single(workspaceStateGenerator.UpdateQueue);
            Assert.Equal(update.workspaceProject.Id, ProjectNumberOne.Id);
            Assert.Equal(update.projectSnapshot.FilePath, HostProjectOne.FilePath);
        }

        [ForegroundFact]
        public async Task WorkspaceChanged_DocumentChanged_PartialComponent_UpdatesProjectState_AfterDelay()
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

            var sourceText = SourceText.From(
$@"
public partial class TestComponent : {ComponentsApi.IComponent.MetadataName} {{}}
namespace Microsoft.AspNetCore.Components
{{
    public interface IComponent {{}}
}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // The change detector only operates when a semantic model / syntax tree is available.
            await document.GetSyntaxRootAsync();
            await document.GetSemanticModelAsync();

            var e = new WorkspaceChangeEventArgs(WorkspaceChangeKind.DocumentChanged, oldSolution: solution, newSolution: solution, projectId: ProjectNumberOne.Id, PartialComponentClassDocumentId);
            
            // Act
            detector.Workspace_WorkspaceChanged(Workspace, e);

            // Assert
            //
            // The change hasn't come through yet.
            Assert.Empty(workspaceStateGenerator.UpdateQueue);

            detector.BlockDelayedUpdateWorkEnqueue.Set();

            await detector._deferredUpdates.Single().Value.Task;

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

        [Fact]
        public async Task IsPartialComponentClass_NoIComponent_ReturnsFalse()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(
$@"
public partial class TestComponent{{}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // Initialize document
            await document.GetSyntaxRootAsync();
            await document.GetSemanticModelAsync();

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPartialComponentClass_InitializedDocument_ReturnsTrue()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(
$@"
public partial class TestComponent : {ComponentsApi.IComponent.MetadataName} {{}}
namespace Microsoft.AspNetCore.Components
{{
    public interface IComponent {{}}
}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // Initialize document
            await document.GetSyntaxRootAsync();
            await document.GetSemanticModelAsync();

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPartialComponentClass_Uninitialized_ReturnsFalse()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(
$@"
public partial class TestComponent : {ComponentsApi.IComponent.MetadataName} {{}}
namespace Microsoft.AspNetCore.Components
{{
    public interface IComponent {{}}
}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPartialComponentClass_UninitializedSemanticModel_ReturnsFalse()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(
$@"
public partial class TestComponent : {ComponentsApi.IComponent.MetadataName} {{}}
namespace Microsoft.AspNetCore.Components
{{
    public interface IComponent {{}}
}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            await document.GetSyntaxRootAsync();

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPartialComponentClass_NonClass_ReturnsFalse()
        {
            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(string.Empty);
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // Initialize document
            await document.GetSyntaxRootAsync();
            await document.GetSemanticModelAsync();

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPartialComponentClass_MultipleClassesOneComponentPartial_ReturnsTrue()
        {

            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(
$@"
public partial class NonComponent1 {{}}
public class NonComponent2 {{}}
public partial class TestComponent : {ComponentsApi.IComponent.MetadataName} {{}}
public partial class NonComponent3 {{}}
public class NonComponent4 {{}}
namespace Microsoft.AspNetCore.Components
{{
    public interface IComponent {{}}
}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // Initialize document
            await document.GetSyntaxRootAsync();
            await document.GetSemanticModelAsync();

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsPartialComponentClass_NonComponents_ReturnsFalse()
        {

            // Arrange
            var workspaceStateGenerator = new TestProjectWorkspaceStateGenerator();
            var detector = new WorkspaceProjectStateChangeDetector(workspaceStateGenerator);
            var sourceText = SourceText.From(
$@"
public partial class NonComponent1 {{}}
public class NonComponent2 {{}}
public partial class NonComponent3 {{}}
public class NonComponent4 {{}}
namespace Microsoft.AspNetCore.Components
{{
    public interface IComponent {{}}
}}
");
            var syntaxTreeRoot = CSharpSyntaxTree.ParseText(sourceText).GetRoot();
            var solution = SolutionWithTwoProjects
                .WithDocumentText(PartialComponentClassDocumentId, sourceText)
                .WithDocumentSyntaxRoot(PartialComponentClassDocumentId, syntaxTreeRoot, PreservationMode.PreserveIdentity);
            var document = solution.GetDocument(PartialComponentClassDocumentId);

            // Initialize document
            await document.GetSyntaxRootAsync();
            await document.GetSemanticModelAsync();

            // Act
            var result = detector.IsPartialComponentClass(document);

            // Assert
            Assert.False(result);
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
