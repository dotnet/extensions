// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class ProjectConfigurationStateSynchronizerTest : LanguageServerTestBase
    {
        [Fact]
        public void ProjectConfigurationFileChanged_Added_CantDeserialize_Noops()
        {
            // Arrange
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, projectService.Object);
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
                "/path/to/project.csproj",
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
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, projectService.Object);
            var jsonFileDeserializer = CreateJsonFileDeserializer(handle);
            var args = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, jsonFileDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(args);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Removed_ResetsProject()
        {
            // Arrange
            var handle = new FullProjectSnapshotHandle(
                "/path/to/project.csproj",
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
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, projectService.Object);
            var jsonFileDeserializer = CreateJsonFileDeserializer(handle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, jsonFileDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);
            var removeArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Removed, Mock.Of<JsonFileDeserializer>());

            // Act
            synchronizer.ProjectConfigurationFileChanged(removeArgs);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Changed_UpdatesProject()
        {
            // Arrange
            var initialHandle = new FullProjectSnapshotHandle(
                "/path/to/project.csproj",
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
                "/path/to/project.csproj",
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
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, projectService.Object);
            var addDeserializer = CreateJsonFileDeserializer(initialHandle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, addDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);
            var changedDeserializer = CreateJsonFileDeserializer(changedHandle);
            var changedArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(changedArgs);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Changed_CantDeserialize_ResetsProject()
        {
            // Arrange
            var initialHandle = new FullProjectSnapshotHandle(
                "/path/to/project.csproj",
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
                "/path/to/project.csproj",
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
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, projectService.Object);
            var addDeserializer = CreateJsonFileDeserializer(initialHandle);
            var addArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Added, addDeserializer);
            synchronizer.ProjectConfigurationFileChanged(addArgs);
            var changedDeserializer = Mock.Of<JsonFileDeserializer>();
            var changedArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(changedArgs);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void ProjectConfigurationFileChanged_Changed_UntrackedProject_Noops()
        {
            // Arrange
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            var synchronizer = new ProjectConfigurationStateSynchronizer(Dispatcher, projectService.Object);
            var changedDeserializer = Mock.Of<JsonFileDeserializer>();
            var changedArgs = new ProjectConfigurationFileChangeEventArgs("/path/to/project.razor.json", RazorFileChangeKind.Changed, changedDeserializer);

            // Act
            synchronizer.ProjectConfigurationFileChanged(changedArgs);

            // Assert
            projectService.VerifyAll();
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
