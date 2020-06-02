// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class PrecompiledRazorPageSuppressorTest : OmniSharpWorkspaceTestBase
    {
        [Fact]
        public void Workspace_WorkspaceChanged_NullFilePath_Noops()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: null);
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentChanged,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new PrecompiledRazorPageSuppressor(Workspace);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.Same(newSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public void Workspace_WorkspaceChanged_CommonCSharpFiles_Noops()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: "/path/file.cs");
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentChanged,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new PrecompiledRazorPageSuppressor(Workspace);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.Same(newSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public void Workspace_WorkspaceChanged_GEndingRazorCSharpFile_Noops()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: "/path/Leg.cshtml.cs");
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentChanged,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new PrecompiledRazorPageSuppressor(Workspace);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.Same(newSolution, Workspace.CurrentSolution);
        }

        [Fact]
        public void Workspace_WorkspaceChanged_RazorTargetAssemblyInfo_RemovesDocument()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: "/path/obj/Debug/netcoreapp3.0/TheApp.RazorTargetAssemblyInfo.cs");
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentChanged,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new PrecompiledRazorPageSuppressor(Workspace);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.NotSame(newSolution, Workspace.CurrentSolution);
            var potentialDocument = Workspace.CurrentSolution.GetDocument(addedDocument.Id);
            Assert.Null(potentialDocument);
        }

        [Fact]
        public void Workspace_WorkspaceChanged_RazorAssemblyInfo_RemovesDocument()
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: "/path/obj/Debug/netcoreapp3.0/TheApp.RazorAssemblyInfo.cs");
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentChanged,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new PrecompiledRazorPageSuppressor(Workspace);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.NotSame(newSolution, Workspace.CurrentSolution);
            var potentialDocument = Workspace.CurrentSolution.GetDocument(addedDocument.Id);
            Assert.Null(potentialDocument);
        }

        [Theory]
        [InlineData(".cshtml.g.cs")]
        [InlineData(".razor.g.cs")]
        [InlineData(".g.cshtml.cs")]
        public void Workspace_WorkspaceChanged_DynamicallyGeneratedDocuments_RemovesDocument(string extension)
        {
            // Arrange
            var originalSolution = Workspace.CurrentSolution;
            var addedDocument = AddRoslynDocument(filePath: "/path/obj/Debug/netcoreapp3.0/Razor/Index" + extension);
            var newSolution = Workspace.CurrentSolution;
            var workspaceChangeEventArgs = new WorkspaceChangeEventArgs(
                WorkspaceChangeKind.DocumentChanged,
                originalSolution,
                newSolution,
                addedDocument.Project.Id,
                addedDocument.Id);
            var processedPublisher = new PrecompiledRazorPageSuppressor(Workspace);

            // Act
            processedPublisher.Workspace_WorkspaceChanged(sender: null, workspaceChangeEventArgs);

            // Assert
            Assert.NotSame(newSolution, Workspace.CurrentSolution);
            var potentialDocument = Workspace.CurrentSolution.GetDocument(addedDocument.Id);
            Assert.Null(potentialDocument);
        }
    }
}
