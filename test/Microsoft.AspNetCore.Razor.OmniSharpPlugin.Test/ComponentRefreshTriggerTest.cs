// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class ComponentRefreshTriggerTest : OmniSharpTestBase
    {
        public ProjectInstanceEvaluator ProjectInstanceEvaluator { get; } = Mock.Of<ProjectInstanceEvaluator>();

        public ProjectInstance ProjectInstance { get; } = new ProjectInstance(ProjectRootElement.Create());

        [Fact]
        public void IsCompileItem_CompileItem_ReturnsTrue()
        {
            // Arrange
            var relativeFilePath = "/path/to/obj/Debug/file.razor.g.cs";
            var projectRootElement = ProjectRootElement.Create("/path/to/project.csproj");
            projectRootElement.AddItem("Compile", relativeFilePath);
            var projectInstance = new ProjectInstance(projectRootElement);

            // Act
            var result = ComponentRefreshTrigger.IsCompileItem(relativeFilePath, projectInstance);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCompileItem_NotCompileItem_ReturnsFalse()
        {
            // Arrange
            var relativeFilePath = "/path/to/obj/Debug/file.razor.g.cs";
            var projectRootElement = ProjectRootElement.Create("/path/to/project.csproj");
            var projectInstance = new ProjectInstance(projectRootElement);

            // Act
            var result = ComponentRefreshTrigger.IsCompileItem(relativeFilePath, projectInstance);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RazorDocumentChangedAsync_Changed_PerformsProjectEvaluation()
        {
            // Arrange
            var projectRootElement = ProjectRootElement.Create("/path/to/project.csproj");
            var projectInstance = new ProjectInstance(projectRootElement);
            var projectInstanceEvaluator = new Mock<ProjectInstanceEvaluator>();
            projectInstanceEvaluator.Setup(evaluator => evaluator.Evaluate(It.IsAny<ProjectInstance>()))
                .Verifiable();
            var projectManager = CreateProjectSnapshotManager();
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, projectInstanceEvaluator.Object, LoggerFactory);
            refreshTrigger.Initialize(projectManager);
            await RunOnForegroundAsync(() =>
            {
                var hostProject = new OmniSharpHostProject(projectInstance.ProjectFileLocation.File, RazorConfiguration.Default);
                projectManager.ProjectAdded(hostProject);
                var hostDocument = new OmniSharpHostDocument("file.cshtml", "file.cshtml", FileKinds.Component);
                projectManager.DocumentAdded(hostProject, hostDocument);
            });
            var args = new RazorFileChangeEventArgs("file.cshtml", "file.cshtml", projectInstance, RazorFileChangeKind.Changed);

            // Act
            await refreshTrigger.RazorDocumentChangedAsync(args);

            // Assert
            projectInstanceEvaluator.VerifyAll();
        }

        [Theory]
        [InlineData(RazorFileChangeKind.Added)]
        [InlineData(RazorFileChangeKind.Removed)]
        public async Task RazorDocumentChangedAsync_AddedRemoved_Noops(RazorFileChangeKind changeKind)
        {
            // Arrange
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, ProjectInstanceEvaluator, LoggerFactory);
            var args = new RazorFileChangeEventArgs("file.cshtml", "file.cshtml", ProjectInstance, changeKind);

            // Act & Assert

            // Jump off the foreground thread
            await refreshTrigger.RazorDocumentChangedAsync(args);
        }

        [Fact]
        public async Task RazorDocumentChangedAsync_Changed_NotComponent_Noops()
        {
            // Arrange
            var projectManager = CreateProjectSnapshotManager();
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, ProjectInstanceEvaluator, LoggerFactory);
            refreshTrigger.Initialize(projectManager);

            await RunOnForegroundAsync(() =>
            {
                var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
                projectManager.ProjectAdded(hostProject);
                var hostDocument = new OmniSharpHostDocument("file.cshtml", "file.cshtml", FileKinds.Legacy);
                projectManager.DocumentAdded(hostProject, hostDocument);
            });

            var args = new RazorFileChangeEventArgs("file.cshtml", "file.cshtml", ProjectInstance, RazorFileChangeKind.Changed);

            // Act & Assert
            await refreshTrigger.RazorDocumentChangedAsync(args);
        }

        [Fact]
        public async Task IsComponentFile_UnknownProject_ReturnsFalse()
        {
            // Arrange
            var projectManager = CreateProjectSnapshotManager();
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, ProjectInstanceEvaluator, LoggerFactory);
            refreshTrigger.Initialize(projectManager);

            // Act
            var result = await RunOnForegroundAsync(() => refreshTrigger.IsComponentFile("file.razor", "/path/to/project.csproj"));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsComponentFile_UnknownDocument_ReturnsFalse()
        {
            // Arrange
            var projectManager = CreateProjectSnapshotManager();
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, ProjectInstanceEvaluator, LoggerFactory);
            refreshTrigger.Initialize(projectManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            await RunOnForegroundAsync(() => projectManager.ProjectAdded(hostProject));

            // Act
            var result = await RunOnForegroundAsync(() => refreshTrigger.IsComponentFile("file.razor", hostProject.FilePath));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsComponentFile_NonComponent_ReturnsFalse()
        {
            // Arrange
            var projectManager = CreateProjectSnapshotManager();
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, ProjectInstanceEvaluator, LoggerFactory);
            refreshTrigger.Initialize(projectManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            var hostDocument = new OmniSharpHostDocument("file.cshtml", "file.cshtml", FileKinds.Legacy);
            await RunOnForegroundAsync(() =>
            {
                projectManager.ProjectAdded(hostProject);
                projectManager.DocumentAdded(hostProject, hostDocument);
            });

            // Act
            var result = await RunOnForegroundAsync(() => refreshTrigger.IsComponentFile(hostDocument.FilePath, hostProject.FilePath));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsComponentFile_Component_ReturnsTrue()
        {
            // Arrange
            var projectManager = CreateProjectSnapshotManager();
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, ProjectInstanceEvaluator, LoggerFactory);
            refreshTrigger.Initialize(projectManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            var hostDocument = new OmniSharpHostDocument("file.cshtml", "file.cshtml", FileKinds.Component);
            await RunOnForegroundAsync(() =>
            {
                projectManager.ProjectAdded(hostProject);
                projectManager.DocumentAdded(hostProject, hostDocument);
            });

            // Act
            var result = await RunOnForegroundAsync(() => refreshTrigger.IsComponentFile(hostDocument.FilePath, hostProject.FilePath));

            // Assert
            Assert.True(result);
        }
    }
}
