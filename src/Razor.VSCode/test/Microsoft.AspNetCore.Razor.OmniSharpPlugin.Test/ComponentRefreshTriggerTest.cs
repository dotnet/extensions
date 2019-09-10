// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using OmniSharp.Models;
using OmniSharp.Models.UpdateBuffer;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class ComponentRefreshTriggerTest : OmniSharpTestBase
    {
        public ComponentRefreshTriggerTest()
        {
            var projectInstanceEvaluator = new Mock<ProjectInstanceEvaluator>();
            projectInstanceEvaluator.Setup(instance => instance.Evaluate(It.IsAny<ProjectInstance>()))
                .Returns<ProjectInstance>(pi => pi);
            ProjectInstanceEvaluator = projectInstanceEvaluator.Object;
        }

        private ProjectInstanceEvaluator ProjectInstanceEvaluator { get; }

        private ProjectInstance ProjectInstance { get; } = new ProjectInstance(ProjectRootElement.Create());

        [Fact]
        public void RazorDocumentOutputChanged_ClearsWorkspaceBufferOnRemove()
        {
            // Arrange
            var updateBufferRequests = new List<Request>();
            var refreshTrigger = CreateTestComponentRefreshTrigger(onUpdateBuffer: (request) => updateBufferRequests.Add(request));
            refreshTrigger.BlockRefreshWorkStarting = new ManualResetEventSlim(initialState: false);
            var filePath = "file.razor.g.cs";
            var projectRootElement = ProjectRootElement.Create("/path/to/project.csproj");
            projectRootElement.AddItem("Compile", filePath);
            var projectInstance = new ProjectInstance(projectRootElement);
            var onAddArgs = new RazorFileChangeEventArgs(filePath, filePath, projectInstance, RazorFileChangeKind.Added);
            var onRemoveArgs = new RazorFileChangeEventArgs(filePath, filePath, ProjectInstance, RazorFileChangeKind.Removed);
            refreshTrigger.RazorDocumentOutputChanged(onAddArgs);
            refreshTrigger.BlockRefreshWorkStarting.Set();

            refreshTrigger.NotifyRefreshWorkCompleting.Wait(TimeSpan.FromSeconds(1));
            refreshTrigger.NotifyRefreshWorkCompleting.Reset();

            // Act
            refreshTrigger.RazorDocumentOutputChanged(onRemoveArgs);

            // Assert
            refreshTrigger.BlockRefreshWorkStarting.Set();

            refreshTrigger.NotifyRefreshWorkCompleting.Wait(TimeSpan.FromSeconds(1));
            Assert.True(refreshTrigger.NotifyRefreshWorkCompleting.IsSet);

            Assert.Collection(updateBufferRequests,
                request =>
                {
                    var updateBufferRequest = Assert.IsType<UpdateBufferRequest>(request);
                    Assert.Equal(filePath, updateBufferRequest.FileName);
                    Assert.True(updateBufferRequest.FromDisk);
                },
                request =>
                {
                    Assert.Equal(filePath, request.FileName);
                    Assert.Equal(string.Empty, request.Buffer);
                });
        }

        [Fact]
        public void RazorDocumentOutputChanged_BatchesFileUpdates()
        {
            // Arrange
            var updateBufferRequests = new List<Request>();
            var refreshTrigger = CreateTestComponentRefreshTrigger(onUpdateBuffer: (request) => updateBufferRequests.Add(request));
            refreshTrigger.BlockRefreshWorkStarting = new ManualResetEventSlim(initialState: false);
            var file1Path = "file.razor.g.cs";
            var file2Path = "anotherfile.razor.g.cs";
            var file3Path = "file.razor.g.cs";
            var projectRootElement = ProjectRootElement.Create("/path/to/project.csproj");
            projectRootElement.AddItem("Compile", file1Path);
            projectRootElement.AddItem("Compile", file2Path);
            // Not adding file3 here to ensure it doesn't get updated.

            var projectInstance = new ProjectInstance(projectRootElement);
            var file1Args = new RazorFileChangeEventArgs(file1Path, file1Path, projectInstance, RazorFileChangeKind.Changed);
            var file2Args = new RazorFileChangeEventArgs(file2Path, file2Path, projectInstance, RazorFileChangeKind.Changed);
            var file3Args = new RazorFileChangeEventArgs(file3Path, file3Path, projectInstance, RazorFileChangeKind.Changed);

            // Act
            refreshTrigger.RazorDocumentOutputChanged(file1Args);
            refreshTrigger.RazorDocumentOutputChanged(file2Args);
            refreshTrigger.RazorDocumentOutputChanged(file3Args);

            // Assert
            refreshTrigger.BlockRefreshWorkStarting.Set();

            refreshTrigger.NotifyRefreshWorkCompleting.Wait(TimeSpan.FromSeconds(1));
            Assert.True(refreshTrigger.NotifyRefreshWorkCompleting.IsSet);

            Assert.Collection(updateBufferRequests,
                request =>
                {
                    var updateBufferRequest = Assert.IsType<UpdateBufferRequest>(request);
                    Assert.Equal(file1Path, updateBufferRequest.FileName);
                    Assert.True(updateBufferRequest.FromDisk);
                },
                request =>
                {
                    var updateBufferRequest = Assert.IsType<UpdateBufferRequest>(request);
                    Assert.Equal(file2Path, updateBufferRequest.FileName);
                    Assert.True(updateBufferRequest.FromDisk);
                });
        }

        [Fact]
        public void RazorDocumentOutputChanged_MemoizesRefreshTasks()
        {
            // Arrange
            var refreshTrigger = CreateTestComponentRefreshTrigger();
            refreshTrigger.BlockRefreshWorkStarting = new ManualResetEventSlim(initialState: false);
            var args = new RazorFileChangeEventArgs("/path/to/file.razor.g.cs", "file.razor.g.cs", ProjectInstance, RazorFileChangeKind.Added);

            // Act
            refreshTrigger.RazorDocumentOutputChanged(args);
            refreshTrigger.RazorDocumentOutputChanged(args);

            // Assert
            Assert.Single(refreshTrigger._deferredRefreshTasks);

            refreshTrigger.BlockRefreshWorkStarting.Set();

            refreshTrigger.NotifyRefreshWorkCompleting.Wait(TimeSpan.FromSeconds(1));
            Assert.True(refreshTrigger.NotifyRefreshWorkCompleting.IsSet);
        }

        [Fact]
        public void RazorDocumentOutputChanged_EnqueuesRefresh()
        {
            // Arrange
            var refreshTrigger = CreateTestComponentRefreshTrigger();
            var args = new RazorFileChangeEventArgs("/path/to/file.razor.g.cs", "file.razor.g.cs", ProjectInstance, RazorFileChangeKind.Added);

            // Act
            refreshTrigger.RazorDocumentOutputChanged(args);

            // Assert
            refreshTrigger.NotifyRefreshWorkStarting.Wait(TimeSpan.FromSeconds(1));
            Assert.True(refreshTrigger.NotifyRefreshWorkStarting.IsSet);

            // Let refresh work complete
            refreshTrigger.NotifyRefreshWorkCompleting.Wait(TimeSpan.FromSeconds(1));
            Assert.True(refreshTrigger.NotifyRefreshWorkCompleting.IsSet);
        }

        [Fact]
        public void IsCompileItem_CompileItem_ReturnsTrue()
        {
            // Arrange
            var relativeFilePath = "/path/to/obj/Debug/file.razor.g.cs";
            var projectRootElement = ProjectRootElement.Create("/path/to/project.csproj");
            projectRootElement.AddItem("Compile", relativeFilePath);
            var projectInstance = new ProjectInstance(projectRootElement);
            var refreshTrigger = CreateTestComponentRefreshTrigger();

            // Act
            var result = refreshTrigger.IsCompileItem(relativeFilePath, projectInstance);

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
            var refreshTrigger = CreateTestComponentRefreshTrigger();

            // Act
            var result = refreshTrigger.IsCompileItem(relativeFilePath, projectInstance);

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
            var updateBufferDispatcher = Mock.Of<UpdateBufferDispatcher>(dispatcher => dispatcher.UpdateBufferAsync(It.IsAny<Request>()) == Task.CompletedTask);
            var refreshTrigger = new ComponentRefreshTrigger(Dispatcher, new FilePathNormalizer(), projectInstanceEvaluator.Object, updateBufferDispatcher, LoggerFactory);
            refreshTrigger.Initialize(projectManager);
            await RunOnForegroundAsync(() =>
            {
                var hostProject = new OmniSharpHostProject(projectInstance.ProjectFileLocation.File, RazorConfiguration.Default, "TestRootNamespace");
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
            var refreshTrigger = CreateTestComponentRefreshTrigger();
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
            var refreshTrigger = CreateTestComponentRefreshTrigger();
            refreshTrigger.Initialize(projectManager);

            await RunOnForegroundAsync(() =>
            {
                var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
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
            var refreshTrigger = CreateTestComponentRefreshTrigger();
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
            var refreshTrigger = CreateTestComponentRefreshTrigger();
            refreshTrigger.Initialize(projectManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
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
            var refreshTrigger = CreateTestComponentRefreshTrigger();
            refreshTrigger.Initialize(projectManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
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
            var refreshTrigger = CreateTestComponentRefreshTrigger();
            refreshTrigger.Initialize(projectManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
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

        private ComponentRefreshTrigger CreateTestComponentRefreshTrigger(Action<Request> onUpdateBuffer = null)
        {
            onUpdateBuffer = onUpdateBuffer ?? ((_) => { });
            var testUpdateBufferDispatcher = new TestUpdateBufferDispatcher(onUpdateBuffer);
            return new ComponentRefreshTrigger(Dispatcher, new FilePathNormalizer(), ProjectInstanceEvaluator, testUpdateBufferDispatcher, LoggerFactory)
            {
                EnqueueDelay = 1,
                NotifyRefreshWorkStarting = new ManualResetEventSlim(initialState: false),
                NotifyRefreshWorkCompleting = new ManualResetEventSlim(initialState: false),
            };
        }

        private class TestUpdateBufferDispatcher : UpdateBufferDispatcher
        {
            private readonly Action<Request> _onUpdateBuffer;

            public TestUpdateBufferDispatcher(Action<Request> onUpdateBuffer)
            {
                if (onUpdateBuffer == null)
                {
                    throw new ArgumentNullException(nameof(onUpdateBuffer));
                }

                _onUpdateBuffer = onUpdateBuffer;
            }

            public override Task UpdateBufferAsync(Request request)
            {
                _onUpdateBuffer(request);

                return Task.CompletedTask;
            }
        }
    }
}
