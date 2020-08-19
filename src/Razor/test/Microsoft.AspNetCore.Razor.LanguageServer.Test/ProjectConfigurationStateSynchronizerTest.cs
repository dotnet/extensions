// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces.Serialization;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class ProjectConfigurationStateSynchronizerTest : LanguageServerTestBase
    {

        [Fact]
        public void ProjectConfigurationFileChanged_Removed_UnknownDocumentNoops()
        {
            // Arrange
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            var synchronizer = GetSynchronizer(projectService.Object);
            var jsonFileDeserializer = Mock.Of<JsonFileDeserializer>();
            var args = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Removed, jsonFileDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(args);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Removed_NonNormalizedPaths()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Default,
                rootNamespace: "TestRootNamespace",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp5),
                Array.Empty<DocumentSnapshotHandle>());
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(handle.FilePath)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                handle.FilePath,
                handle.Configuration,
                handle.RootNamespace,
                handle.ProjectWorkspaceState,
                handle.Documents)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                 handle.FilePath,
                 null,
                 null,
                 ProjectWorkspaceState.Default,
                 Array.Empty<DocumentSnapshotHandle>())).Verifiable();
            var synchronizer = GetSynchronizer(projectService.Object);
            var jsonFileDeserializer = CreateJsonFileDeserializer(handle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("/path/to\\project.razor.json", RazorFileChangeKind.Added, jsonFileDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);
            WaitForEnqueue(synchronizer).Wait();
            var removeArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Removed, Mock.Of<JsonFileDeserializer>());

            // Act
            synchronizer.ProjectConfigurationFileChanged(removeArgs);
            WaitForEnqueue(synchronizer).Wait();

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Added_CantDeserialize_Noops()
        {
            // Arrange
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            var synchronizer = GetSynchronizer(projectService.Object);
            var jsonFileDeserializer = Mock.Of<JsonFileDeserializer>();
            var args = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, jsonFileDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(args);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Added_AddAndUpdatesProject()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Default,
                rootNamespace: "TestRootNamespace",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp5),
                Array.Empty<DocumentSnapshotHandle>());
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(handle.FilePath)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                handle.FilePath,
                handle.Configuration,
                handle.RootNamespace,
                handle.ProjectWorkspaceState,
                handle.Documents)).Verifiable();
            var synchronizer = GetSynchronizer(projectService.Object);
            var jsonFileDeserializer = CreateJsonFileDeserializer(handle);
            var args = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, jsonFileDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(args);
            WaitForEnqueue(synchronizer).Wait();

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Removed_ResetsProject()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Default,
                rootNamespace: "TestRootNamespace",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp5),
                Array.Empty<DocumentSnapshotHandle>());
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(handle.FilePath)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                handle.FilePath,
                handle.Configuration,
                handle.RootNamespace,
                handle.ProjectWorkspaceState,
                handle.Documents)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                 handle.FilePath,
                 null,
                 null,
                 ProjectWorkspaceState.Default,
                 Array.Empty<DocumentSnapshotHandle>())).Verifiable();
            var synchronizer = GetSynchronizer(projectService.Object);
            var jsonFileDeserializer = CreateJsonFileDeserializer(handle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, jsonFileDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);
            WaitForEnqueue(synchronizer).Wait();
            var removeArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Removed, Mock.Of<JsonFileDeserializer>());

            // Act
            synchronizer.ProjectConfigurationFileChanged(removeArgs);
            WaitForEnqueue(synchronizer).Wait();

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Changed_UpdatesProject()
        {
            // Arrange
            var initialHandle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Default,
                rootNamespace: "TestRootNamespace",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp5),
                Array.Empty<DocumentSnapshotHandle>());
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(initialHandle.FilePath)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                initialHandle.FilePath,
                initialHandle.Configuration,
                initialHandle.RootNamespace,
                initialHandle.ProjectWorkspaceState,
                initialHandle.Documents)).Verifiable();
            var changedHandle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Create(
                    RazorLanguageVersion.Experimental,
                    "TestConfiguration",
                    Array.Empty<RazorExtension>()),
                rootNamespace: "TestRootNamespace2",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp6),
                Array.Empty<DocumentSnapshotHandle>());
            projectService.Setup(service => service.UpdateProject(
                changedHandle.FilePath,
                changedHandle.Configuration,
                changedHandle.RootNamespace,
                changedHandle.ProjectWorkspaceState,
                changedHandle.Documents)).Verifiable();
            var synchronizer = GetSynchronizer(projectService.Object);
            var addDeserializer = CreateJsonFileDeserializer(initialHandle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("path/to/project.razor.json", RazorFileChangeKind.Added, addDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);

            WaitForEnqueue(synchronizer).Wait();

            var changedDeserializer = CreateJsonFileDeserializer(changedHandle);
            var changedArgs = new ProjectConfigurationFileChangeEventArgs("path/to/project.razor.json", RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(changedArgs);
            WaitForEnqueue(synchronizer).Wait();

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Changed_CantDeserialize_ResetsProject()
        {
            // Arrange
            var initialHandle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Default,
                rootNamespace: "TestRootNamespace",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp5),
                Array.Empty<DocumentSnapshotHandle>());
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(initialHandle.FilePath)).Verifiable();
            projectService.Setup(service => service.UpdateProject(
                initialHandle.FilePath,
                initialHandle.Configuration,
                initialHandle.RootNamespace,
                initialHandle.ProjectWorkspaceState,
                initialHandle.Documents)).Verifiable();
            var changedHandle = new FullProjectSnapshotHandle(
                "path/to/project.csproj",
                RazorConfiguration.Create(
                    RazorLanguageVersion.Experimental,
                    "TestConfiguration",
                    Array.Empty<RazorExtension>()),
                rootNamespace: "TestRootNamespace2",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp6),
                Array.Empty<DocumentSnapshotHandle>());

            // This is the request that happens when the server is reset
            projectService.Setup(service => service.UpdateProject(
                 initialHandle.FilePath,
                 null,
                 null,
                 ProjectWorkspaceState.Default,
                 Array.Empty<DocumentSnapshotHandle>())).Verifiable();
            var synchronizer = GetSynchronizer(projectService.Object);
            var addDeserializer = CreateJsonFileDeserializer(initialHandle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, addDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);
            WaitForEnqueue(synchronizer).Wait();
            var changedDeserializer = Mock.Of<JsonFileDeserializer>();
            var changedArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(changedArgs);
            WaitForEnqueue(synchronizer).Wait();

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Changed_UntrackedProject_Noops()
        {
            // Arrange
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            var synchronizer = GetSynchronizer(projectService.Object);
            var changedDeserializer = Mock.Of<JsonFileDeserializer>();
            var changedArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(changedArgs);
            WaitForEnqueue(synchronizer, hasTask: false).Wait();

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_RemoveThenAdd_OnlyAdds()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
            "path/to/project.csproj",
                RazorConfiguration.Default,
                rootNamespace: "TestRootNamespace",
                new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.CSharp5),
                Array.Empty<DocumentSnapshotHandle>());

            var filePath = "path/to/project.csproj";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddProject(handle.FilePath)).Verifiable();
            projectService.Setup(p => p.UpdateProject(
                filePath,
                It.IsAny<RazorConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<ProjectWorkspaceState>(),
                It.IsAny<IReadOnlyList<DocumentSnapshotHandle>>()));

            var synchronizer = GetSynchronizer(projectService.Object);
            var changedDeserializer = CreateJsonFileDeserializer(handle);
            var removedArgs = new ProjectConfigurationFileChangeEventArgs(filePath, RazorFileChangeKind.Removed, changedDeserializer);
            var addedArgs = new ProjectConfigurationFileChangeEventArgs(filePath, RazorFileChangeKind.Added, changedDeserializer);
            var changedArgs = new ProjectConfigurationFileChangeEventArgs(filePath, RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(addedArgs);
            synchronizer.ProjectConfigurationFileChanged(changedArgs);
            WaitForEnqueue(synchronizer).Wait();

            // Assert
            projectService.Verify(p => p.UpdateProject(
                filePath,
                It.IsAny<RazorConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<ProjectWorkspaceState>(),
                It.IsAny<IReadOnlyList<DocumentSnapshotHandle>>()), Times.Once);

            projectService.VerifyAll();
        }

        private async Task WaitForEnqueue(ProjectConfigurationStateSynchronizer synchronizer, bool hasTask = true)
        {
            if (hasTask)
            {
                var kvp = Assert.Single(synchronizer._projectInfoMap);
                await Task.Factory.StartNew(() =>
                {
                    kvp.Value.ProjectUpdateTask.Wait();
                }, CancellationToken.None, TaskCreationOptions.None, Dispatcher.ForegroundScheduler);
            }
            else
            {
                Assert.Empty(synchronizer._projectInfoMap);
            }
        }

        private ProjectConfigurationStateSynchronizer GetSynchronizer(RazorProjectService razorProjectService)
        {
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, razorProjectService, FilePathNormalizer);
            synchronizer.EnqueueDelay = 5;

            return synchronizer;
        }

        private JsonFileDeserializer CreateJsonFileDeserializer(FullProjectSnapshotHandle deserializedHandle)
        {
            var deserializer = new Mock<JsonFileDeserializer>();
            deserializer.Setup(deserializer => deserializer.Deserialize<FullProjectSnapshotHandle>(It.IsAny<string>()))
                .Returns(deserializedHandle);

            return deserializer.Object;
        }
    }
}
