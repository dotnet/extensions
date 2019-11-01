// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentProjectResolverTest : LanguageServerTestBase
    {
        [Fact]
        public void TryResolvePotentialProject_NoProjects_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var projectResolver = CreateProjectResolver(() => new ProjectSnapshot[0]);

            // Act
            var result = projectResolver.TryResolvePotentialProject(documentFilePath, out var project);

            // Assert
            Assert.False(result);
            Assert.Null(project);
        }

        [Fact]
        public void TryResolvePotentialProject_OnlyMiscellaneousProject_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            DefaultProjectResolver projectResolver = null;
            var miscProject = new Mock<ProjectSnapshot>();
            miscProject.Setup(p => p.FilePath)
                .Returns(() => projectResolver._miscellaneousHostProject.FilePath);
            projectResolver = CreateProjectResolver(() => new[] { miscProject.Object });

            // Act
            var result = projectResolver.TryResolvePotentialProject(documentFilePath, out var project);

            // Assert
            Assert.False(result);
            Assert.Null(project);
        }

        [Fact]
        public void TryResolvePotentialProject_UnrelatedProject_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var unrelatedProject = Mock.Of<ProjectSnapshot>(p => p.FilePath == "C:/other/path/to/project.csproj");
            var projectResolver = CreateProjectResolver(() => new[] { unrelatedProject });

            // Act
            var result = projectResolver.TryResolvePotentialProject(documentFilePath, out var project);

            // Assert
            Assert.False(result);
            Assert.Null(project);
        }

        [Fact]
        public void TryResolvePotentialProject_OwnerProjectWithOthers_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var unrelatedProject = Mock.Of<ProjectSnapshot>(p => p.FilePath == "C:/other/path/to/project.csproj");
            var ownerProject = Mock.Of<ProjectSnapshot>(p => p.FilePath == "C:/path/to/project.csproj");
            var projectResolver = CreateProjectResolver(() => new[] { unrelatedProject, ownerProject });

            // Act
            var result = projectResolver.TryResolvePotentialProject(documentFilePath, out var project);

            // Assert
            Assert.True(result);
            Assert.Same(ownerProject, project);
        }

        [Fact]
        public void GetMiscellaneousProject_ProjectLoaded_ReturnsExistingProject()
        {
            // Arrange
            DefaultProjectResolver projectResolver = null;
            var miscProject = new Mock<ProjectSnapshot>();
            miscProject.Setup(p => p.FilePath)
                .Returns(() => projectResolver._miscellaneousHostProject.FilePath);
            var expectedProject = miscProject.Object;
            projectResolver = CreateProjectResolver(() => new[] { expectedProject });

            // Act
            var project = projectResolver.GetMiscellaneousProject();

            // Assert
            Assert.Same(expectedProject, project);
        }

        [Fact]
        public void GetMiscellaneousProject_ProjectNotLoaded_CreatesProjectAndReturnsCreatedProject()
        {
            // Arrange
            DefaultProjectResolver projectResolver = null;
            var projects = new List<ProjectSnapshot>();
            var filePathNormalizer = new FilePathNormalizer();
            var snapshotManager = new Mock<ProjectSnapshotManagerBase>();
            snapshotManager.Setup(manager => manager.Projects)
                .Returns(() => projects);
            snapshotManager.Setup(manager => manager.GetLoadedProject(It.IsAny<string>()))
                .Returns<string>(filePath => projects.FirstOrDefault(p => p.FilePath == filePath));
            snapshotManager.Setup(manager => manager.ProjectAdded(It.IsAny<HostProject>()))
                .Callback<HostProject>(hostProject => projects.Add(Mock.Of<ProjectSnapshot>(p => p.FilePath == hostProject.FilePath)));
            var snapshotManagerAccessor = Mock.Of<ProjectSnapshotManagerAccessor>(accessor => accessor.Instance == snapshotManager.Object);
            projectResolver = new DefaultProjectResolver(Dispatcher, filePathNormalizer, snapshotManagerAccessor);

            // Act
            var project = projectResolver.GetMiscellaneousProject();

            // Assert
            Assert.Single(projects);
            Assert.Equal(projectResolver._miscellaneousHostProject.FilePath, project.FilePath);
        }

        private DefaultProjectResolver CreateProjectResolver(Func<ProjectSnapshot[]> projectFactory)
        {
            var filePathNormalizer = new FilePathNormalizer();
            var snapshotManager = new Mock<ProjectSnapshotManagerBase>();
            snapshotManager.Setup(manager => manager.Projects)
                .Returns(projectFactory);
            snapshotManager.Setup(manager => manager.GetLoadedProject(It.IsAny<string>()))
                .Returns<string>(filePath => projectFactory().FirstOrDefault(project => project.FilePath == filePath));
            var snapshotManagerAccessor = Mock.Of<ProjectSnapshotManagerAccessor>(accessor => accessor.Instance == snapshotManager.Object);
            var projectResolver = new DefaultProjectResolver(Dispatcher, filePathNormalizer, snapshotManagerAccessor);

            return projectResolver;
        }
    }
}
