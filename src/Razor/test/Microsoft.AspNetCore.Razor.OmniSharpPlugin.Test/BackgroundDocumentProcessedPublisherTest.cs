// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class BackgroundDocumentProcessedPublisherTest : OmniSharpWorkspaceTestBase
    {
        [Fact]
        public async Task DocumentProcessed_Import_Noops()
        {
            // Arrange
            var project = CreateProjectSnapshot(Project.FilePath, new[] { "/path/to/_Imports.razor" });
            var document = project.GetDocument("/path/to/_Imports.razor");
            var originalSolution = Workspace.CurrentSolution;
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            await RunOnForegroundAsync(() => processedPublisher.DocumentProcessed(document));

            // Assert
            Assert.Same(originalSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public async Task DocumentProcessed_WorkspaceHasActiveDocument_Noops()
        {
            // Arrange
            var project = CreateProjectSnapshot(Project.FilePath, new[] { "/path/to/Counter.razor" });
            var document = project.GetDocument("/path/to/Counter.razor");
            var activeDocumentFilePath = document.FilePath + BackgroundDocumentProcessedPublisher.ActiveVirtualDocumentSuffix;
            AddRoslynDocument(activeDocumentFilePath);
            var originalSolution = Workspace.CurrentSolution;
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            await RunOnForegroundAsync(() => processedPublisher.DocumentProcessed(document));

            // Assert
            Assert.Same(originalSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public async Task DocumentProcessed_NoActiveDocument_UnknownProject_Noops()
        {
            // Arrange
            var project = CreateProjectSnapshot("/path/to/unknownproject.csproj", new[] { "/path/to/Counter.razor" });
            var document = project.GetDocument("/path/to/Counter.razor");
            var originalSolution = Workspace.CurrentSolution;
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            await RunOnForegroundAsync(() => processedPublisher.DocumentProcessed(document));

            // Assert
            Assert.Same(originalSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public async Task DocumentProcessed_NoActiveDocument_AddsRazorDocument()
        {
            // Arrange
            var projectSnapshot = CreateProjectSnapshot(Project.FilePath, new[] { "/path/to/Counter.razor" });
            var document = projectSnapshot.GetDocument("/path/to/Counter.razor");
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            await RunOnForegroundAsync(() => processedPublisher.DocumentProcessed(document));

            // Assert
            var project = Assert.Single(Workspace.CurrentSolution.Projects);
            Assert.Contains(project.Documents, roslynDocument => roslynDocument.FilePath.StartsWith(document.FilePath, StringComparison.Ordinal));
        }

        [Fact]
        public async Task DocumentProcessed_NoActiveDocument_AddsCSHTMLDocument()
        {
            // Arrange
            var projectSnapshot = CreateProjectSnapshot(Project.FilePath, new[] { "/path/to/Index.cshtml" });
            var document = projectSnapshot.GetDocument("/path/to/Index.cshtml");
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            await RunOnForegroundAsync(() => processedPublisher.DocumentProcessed(document));

            // Assert
            var project = Assert.Single(Workspace.CurrentSolution.Projects);
            Assert.Contains(project.Documents, roslynDocument => roslynDocument.FilePath.StartsWith(document.FilePath, StringComparison.Ordinal));
        }

        [Fact]
        public async Task DocumentProcessed_NoActiveDocument_ExistingBGDoc_UpdatesDocument()
        {
            // Arrange
            var project = CreateProjectSnapshot(Project.FilePath, new[] { "/path/to/Counter.razor" });
            var document = project.GetDocument("/path/to/Counter.razor");
            var backgroundDocumentFilePath = document.FilePath + BackgroundDocumentProcessedPublisher.BackgroundVirtualDocumentSuffix;
            var currentDocument = AddRoslynDocument(backgroundDocumentFilePath);
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            await RunOnForegroundAsync(() => processedPublisher.DocumentProcessed(document));

            // Assert
            var afterProcessedDocument = Workspace.GetDocument(backgroundDocumentFilePath);
            Assert.NotSame(currentDocument, afterProcessedDocument);
        }

        [Fact]
        public async Task PSM_DocumentRemoved_UnknownProjectForDocument_Noops()
        {
            // Arrange
            var projectSnapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var hostProject = new OmniSharpHostProject("/path/to/unknownproject.csproj", RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
            var hostDocument = new OmniSharpHostDocument("/path/to/Counter.razor", "path\\to\\Counter.razor", FileKinds.Component);
            await RunOnForegroundAsync(() =>
            {
                projectSnapshotManager.ProjectAdded(hostProject);
                projectSnapshotManager.DocumentAdded(hostProject, hostDocument);
            });
            var originalSolution = Workspace.CurrentSolution;
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);
            processedPublisher.Initialize(projectSnapshotManager);

            // Act
            await RunOnForegroundAsync(() => projectSnapshotManager.DocumentRemoved(hostProject, hostDocument));

            // Assert
            Assert.Same(originalSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public async Task PSM_DocumentRemoved_NoBackgroundDocument_Noops()
        {
            // Arrange
            var projectSnapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var hostProject = new OmniSharpHostProject(Project.FilePath, RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
            var hostDocument = new OmniSharpHostDocument("/path/to/Counter.razor", "path\\to\\Counter.razor", FileKinds.Component);
            await RunOnForegroundAsync(() =>
            {
                projectSnapshotManager.ProjectAdded(hostProject);
                projectSnapshotManager.DocumentAdded(hostProject, hostDocument);
            });
            var originalSolution = Workspace.CurrentSolution;
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);
            processedPublisher.Initialize(projectSnapshotManager);

            // Act
            await RunOnForegroundAsync(() => projectSnapshotManager.DocumentRemoved(hostProject, hostDocument));

            // Assert
            Assert.Same(originalSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public async Task PSM_DocumentRemoved_RemovesAssociatedBackgroundDocument()
        {
            // Arrange
            var projectSnapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var hostProject = new OmniSharpHostProject(Project.FilePath, RazorConfiguration.Default, rootNamespace: "TestRootNamespace");
            var hostDocument = new OmniSharpHostDocument("/path/to/Counter.razor", "path\\to\\Counter.razor", FileKinds.Component);
            await RunOnForegroundAsync(() =>
            {
                projectSnapshotManager.ProjectAdded(hostProject);
                projectSnapshotManager.DocumentAdded(hostProject, hostDocument);
            });
            var backgroundDocumentFilePath = hostDocument.FilePath + BackgroundDocumentProcessedPublisher.BackgroundVirtualDocumentSuffix;
            AddRoslynDocument(backgroundDocumentFilePath);
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);
            processedPublisher.Initialize(projectSnapshotManager);

            // Act
            await RunOnForegroundAsync(() => projectSnapshotManager.DocumentRemoved(hostProject, hostDocument));

            // Assert
            var project = Assert.Single(Workspace.CurrentSolution.Projects);
            Assert.Empty(project.Documents);
        }

        [Fact]
        public void WorkspaceChanged_DocumentAdded_NoFilePathRoslynDocument_Noops()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: null);
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentAdded,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.Same(newSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public void WorkspaceChanged_BackgroundDocument_Noops()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument("/path/to/Counter.razor" + BackgroundDocumentProcessedPublisher.BackgroundVirtualDocumentSuffix);
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentAdded,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.Same(newSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public void WorkspaceChanged_ActiveDocument_NoBackgroundDocument_Noops()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument("/path/to/Counter.razor" + BackgroundDocumentProcessedPublisher.ActiveVirtualDocumentSuffix);
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentAdded,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.Same(newSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public void WorkspaceChanged_ActiveDocument_RemovesBackgroundDocument()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var filePath = "/path/to/Counter.razor";
            var backgroundDocumentFilePath = filePath + BackgroundDocumentProcessedPublisher.BackgroundVirtualDocumentSuffix;
            var backgroundDocument = AddRoslynDocument(backgroundDocumentFilePath);
            var activeDocument = AddRoslynDocument(filePath + BackgroundDocumentProcessedPublisher.ActiveVirtualDocumentSuffix);
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentAdded,
                originalSolution,
                newSolution,
                activeDocument.Project.Id,
                activeDocument.Id);
            var processedPublisher = new BackgroundDocumentProcessedPublisher(Dispatcher, Workspace, LoggerFactory);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.NotSame(newSolution, Workspace.CurrentSolution);
            var currentBackgroundDocument = Workspace.CurrentSolution.GetDocument(backgroundDocument.Id);
            Assert.Null(currentBackgroundDocument);
        }
    }
}
