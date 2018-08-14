// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultRazorProjectServiceTest : TestBase
    {
        [Fact]
        public void AddDocument_AddsDocumentToOwnerProject()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var ownerProject = new TestProjectSnapshot("C:/path/to/project.sproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>
                {
                    [documentFilePath] = ownerProject
                },
                new TestProjectSnapshot("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>(), It.IsAny<TextLoader>()))
                .Callback<HostProjectShim, HostDocumentShim, TextLoader>((hostProject, hostDocumentShim, textLoader) =>
                {
                    Assert.Same(ownerProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocumentShim.FilePath);
                    Assert.NotNull(textLoader);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var sourceText = SourceText.From("Hello World");

            // Act
            projectService.AddDocument(sourceText, documentFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void AddDocument_AddsDocumentToMiscellaneousProject()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshotShim>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>(), It.IsAny<TextLoader>()))
                .Callback<HostProjectShim, HostDocumentShim, TextLoader>((hostProject, hostDocumentShim, textLoader) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocumentShim.FilePath);
                    Assert.NotNull(textLoader);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);
            var sourceText = SourceText.From("Hello World");

            // Act
            projectService.AddDocument(sourceText, documentFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveDocument_RemovesDocumentFromOwnerProject()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var ownerProject = new TestProjectSnapshot("C:/path/to/project.sproj", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>
                {
                    [documentFilePath] = ownerProject
                },
                new TestProjectSnapshot("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>()))
                .Callback<HostProjectShim, HostDocumentShim>((hostProject, hostDocumentShim) =>
                {
                    Assert.Same(ownerProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocumentShim.FilePath);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.RemoveDocument(documentFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveDocument_RemovesDocumentFromMiscellaneousProject()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MIS_PROJECT__", new[] { documentFilePath });
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>(),
                miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>()))
                .Callback<HostProjectShim, HostDocumentShim>((hostProject, hostDocumentShim) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);
                    Assert.Equal(documentFilePath, hostDocumentShim.FilePath);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.RemoveDocument(documentFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveDocument_NoopsIfOwnerProjectDoesNotContainDocument()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var ownerProject = new TestProjectSnapshot("C:/path/to/project.sproj", new string[0]);
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>
                {
                    [documentFilePath] = ownerProject
                },
                new TestProjectSnapshot("__MISC_PROJECT__"));
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>();
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.RemoveDocument(documentFilePath);
        }

        [Fact]
        public void RemoveDocument_NoopsIfMiscellaneousProjectDoesNotContainDocument()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MIS_PROJECT__", new string[0]);
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>(),
                miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>();
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.RemoveDocument(documentFilePath);
        }

        // TODO: Add UpdateDocument tests. This API will change significantly when we start consuming incremental changes.

        [Fact]
        public void AddProject_AddsProjectWithProvidedConfiguration()
        {
            // Arrange
            var projectConfiguration = RazorConfiguration.Create(RazorLanguageVersion.Version_1_0, "Test", Array.Empty<RazorExtension>());
            var projectFilePath = "C:/path/to/document.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshotShim>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.HostProjectAdded(It.IsAny<HostProjectShim>()))
                .Callback<HostProjectShim>((hostProject) =>
                {
                    Assert.Equal(projectFilePath, hostProject.FilePath);
                    Assert.Same(projectConfiguration, hostProject.Configuration);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.AddProject(projectFilePath, projectConfiguration);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveProject_RemovesProject()
        {
            // Arrange
            var projectFilePath = "C:/path/to/document.cshtml";
            var ownerProject = new TestProjectSnapshot(projectFilePath);
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshotShim>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.GetLoadedProject(projectFilePath))
                .Returns(ownerProject);
            projectSnapshotManager.Setup(manager => manager.HostProjectRemoved(ownerProject.HostProject))
                .Callback<HostProjectShim>((hostProject) =>
                {
                    Assert.Equal(projectFilePath, hostProject.FilePath);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.RemoveProject(projectFilePath);

            // Assert
            projectSnapshotManager.VerifyAll();
        }

        [Fact]
        public void RemoveProject_NoopsIfProjectIsNotLoaded()
        {
            // Arrange
            var projectFilePath = "C:/path/to/document.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__");
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshotShim>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>();
            projectSnapshotManager.Setup(manager => manager.HostProjectRemoved(It.IsAny<HostProjectShim>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.RemoveProject(projectFilePath);
        }

        [Fact]
        public void TryMigrateDocumentsFromRemovedProject_MigratesDocumentsToNonMiscProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__");
            var removedProject = new TestProjectSnapshot("C:/path/to/some/project.csproj", new[] { documentFilePath1, documentFilePath2 });
            var projectToBeMigratedTo = new TestProjectSnapshot("C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>
                {
                    [documentFilePath1] = projectToBeMigratedTo,
                    [documentFilePath2] = projectToBeMigratedTo,
                },
                miscellaneousProject);
            var migratedDocuments = new List<HostDocumentShim>();
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>(), It.IsAny<TextLoader>()))
                .Callback<HostProjectShim, HostDocumentShim, TextLoader>((hostProject, hostDocumentShim, textLoader) =>
                {
                    Assert.Same(projectToBeMigratedTo.HostProject, hostProject);
                    Assert.NotNull(textLoader);

                    migratedDocuments.Add(hostDocumentShim);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.TryMigrateDocumentsFromRemovedProject(removedProject);

            // Assert
            Assert.Collection(migratedDocuments,
                document => Assert.Equal(documentFilePath1, document.FilePath),
                document => Assert.Equal(documentFilePath2, document.FilePath));
        }

        [Fact]
        public void TryMigrateDocumentsFromRemovedProject_MigratesDocumentsToMiscProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__");
            var removedProject = new TestProjectSnapshot("C:/path/to/some/project.csproj", new[] { documentFilePath1, documentFilePath2 });
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshotShim>(), miscellaneousProject);
            var migratedDocuments = new List<HostDocumentShim>();
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>(), It.IsAny<TextLoader>()))
                .Callback<HostProjectShim, HostDocumentShim, TextLoader>((hostProject, hostDocumentShim, textLoader) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);
                    Assert.NotNull(textLoader);

                    migratedDocuments.Add(hostDocumentShim);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.TryMigrateDocumentsFromRemovedProject(removedProject);

            // Assert
            Assert.Collection(migratedDocuments,
                document => Assert.Equal(documentFilePath1, document.FilePath),
                document => Assert.Equal(documentFilePath2, document.FilePath));
        }

        [Fact]
        public void TryMigrateMiscellaneousDocumentsToProject_DoesNotMigrateDocumentsIfNoOwnerProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__", new[] { documentFilePath1, documentFilePath2 });
            var projectResolver = new TestProjectResolver(new Dictionary<string, ProjectSnapshotShim>(), miscellaneousProject);
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>();
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>(), It.IsAny<TextLoader>()))
                .Throws(new InvalidOperationException("Should not have been called."));
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act & Assert
            projectService.TryMigrateMiscellaneousDocumentsToProject();
        }

        [Fact]
        public void TryMigrateMiscellaneousDocumentsToProject_MigratesDocumentsToNewOwnerProject()
        {
            // Arrange
            var documentFilePath1 = "C:/path/to/document1.cshtml";
            var documentFilePath2 = "C:/path/to/document2.cshtml";
            var miscellaneousProject = new TestProjectSnapshot("__MISC_PROJECT__", new[] { documentFilePath1, documentFilePath2 });
            var projectToBeMigratedTo = new TestProjectSnapshot("C:/path/to/project.csproj");
            var projectResolver = new TestProjectResolver(
                new Dictionary<string, ProjectSnapshotShim>
                {
                    [documentFilePath1] = projectToBeMigratedTo,
                    [documentFilePath2] = projectToBeMigratedTo,
                },
                miscellaneousProject);
            var migratedDocuments = new List<HostDocumentShim>();
            var projectSnapshotManager = new Mock<ProjectSnapshotManagerShim>(MockBehavior.Strict);
            projectSnapshotManager.Setup(manager => manager.DocumentAdded(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>(), It.IsAny<TextLoader>()))
                .Callback<HostProjectShim, HostDocumentShim, TextLoader>((hostProject, hostDocumentShim, textLoader) =>
                {
                    Assert.Same(projectToBeMigratedTo.HostProject, hostProject);
                    Assert.NotNull(textLoader);

                    migratedDocuments.Add(hostDocumentShim);
                });
            projectSnapshotManager.Setup(manager => manager.DocumentRemoved(It.IsAny<HostProjectShim>(), It.IsAny<HostDocumentShim>()))
                .Callback<HostProjectShim, HostDocumentShim>((hostProject, hostDocumentShim) =>
                {
                    Assert.Same(miscellaneousProject.HostProject, hostProject);

                    Assert.DoesNotContain(hostDocumentShim, migratedDocuments);
                });
            var projectService = CreateProjectService(projectResolver, projectSnapshotManager.Object);

            // Act
            projectService.TryMigrateMiscellaneousDocumentsToProject();

            // Assert
            Assert.Collection(migratedDocuments,
                document => Assert.Equal(documentFilePath1, document.FilePath),
                document => Assert.Equal(documentFilePath2, document.FilePath));
        }

        private DefaultRazorProjectService CreateProjectService(ProjectResolver projectResolver, ProjectSnapshotManagerShim projectSnapshotManager)
        {
            var logger = Mock.Of<VSCodeLogger>();
            var filePathNormalizer = new FilePathNormalizer();
            var accessor = Mock.Of<ProjectSnapshotManagerShimAccessor>(a => a.Instance == projectSnapshotManager);
            var projectService = new DefaultRazorProjectService(Dispatcher, projectResolver, filePathNormalizer, accessor, logger);

            return projectService;
        }

        private class TestProjectResolver : ProjectResolver
        {
            private readonly IReadOnlyDictionary<string, ProjectSnapshotShim> _projectMappings;
            private readonly ProjectSnapshotShim _miscellaneousProject;

            public TestProjectResolver(IReadOnlyDictionary<string, ProjectSnapshotShim> projectMappings, ProjectSnapshotShim miscellaneousProject)
            {
                _projectMappings = projectMappings;
                _miscellaneousProject = miscellaneousProject;
            }

            public override ProjectSnapshotShim GetMiscellaneousProject() => _miscellaneousProject;

            public override bool TryResolveProject(string documentFilePath, out ProjectSnapshotShim projectSnapshot)
            {
                return _projectMappings.TryGetValue(documentFilePath, out projectSnapshot);
            }
        }
    }
}
