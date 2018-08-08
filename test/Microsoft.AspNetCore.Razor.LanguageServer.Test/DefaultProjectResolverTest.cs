// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentProjectResolverTest : TestBase
    {
        [Fact]
        public void TryResolveProject_NoProjects_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var projectResolver = CreateProjectResolver(() => new ProjectSnapshotShim[0]);

            // Act
            var result = projectResolver.TryResolveProject(documentFilePath, out var project);

            // Assert
            Assert.False(result);
            Assert.Null(project);
        }

        [Fact]
        public void TryResolveProject_OnlyMiscellaneousProject_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            DefaultProjectResolver projectResolver = null;
            var miscProject = new Mock<ProjectSnapshotShim>();
            miscProject.Setup(p => p.FilePath)
                .Returns(() => projectResolver._miscellaneousHostProject.FilePath);
            projectResolver = CreateProjectResolver(() => new[] { miscProject.Object });

            // Act
            var result = projectResolver.TryResolveProject(documentFilePath, out var project);

            // Assert
            Assert.False(result);
            Assert.Null(project);
        }

        [Fact]
        public void TryResolveProject_UnrelatedProject_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var unrelatedProject = Mock.Of<ProjectSnapshotShim>(p => p.FilePath == "C:/other/path/to/project.csproj");
            var projectResolver = CreateProjectResolver(() => new[] { unrelatedProject });

            // Act
            var result = projectResolver.TryResolveProject(documentFilePath, out var project);

            // Assert
            Assert.False(result);
            Assert.Null(project);
        }

        [Fact]
        public void TryResolveProject_OwnerProjectWithOthers_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:/path/to/document.cshtml";
            var unrelatedProject = Mock.Of<ProjectSnapshotShim>(p => p.FilePath == "C:/other/path/to/project.csproj");
            var ownerProject = Mock.Of<ProjectSnapshotShim>(p => p.FilePath == "C:/path/to/project.csproj");
            var projectResolver = CreateProjectResolver(() => new[] { unrelatedProject, ownerProject });

            // Act
            var result = projectResolver.TryResolveProject(documentFilePath, out var project);

            // Assert
            Assert.True(result);
            Assert.Same(ownerProject, project);
        }

        [Fact]
        public void GetMiscellaneousProject_ProjectLoaded_ReturnsExistingProject()
        {
            // Arrange
            DefaultProjectResolver projectResolver = null;
            var miscProject = new Mock<ProjectSnapshotShim>();
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
            var projects = new List<ProjectSnapshotShim>();
            var filePathNormalizer = new FilePathNormalizer();
            var configurationResolver = Mock.Of<RazorConfigurationResolver>(resolver => resolver.Default == RazorConfiguration.Default);
            var snapshotManager = new Mock<ProjectSnapshotManagerShim>();
            snapshotManager.Setup(manager => manager.Projects)
                .Returns(() => projects);
            snapshotManager.Setup(manager => manager.GetLoadedProject(It.IsAny<string>()))
                .Returns<string>(filePath => projects.FirstOrDefault(p => p.FilePath == filePath));
            snapshotManager.Setup(manager => manager.HostProjectAdded(It.IsAny<HostProjectShim>()))
                .Callback<HostProjectShim>(hostProject => projects.Add(Mock.Of<ProjectSnapshotShim>(p => p.FilePath == hostProject.FilePath)));
            var snapshotManagerAccessor = Mock.Of<ProjectSnapshotManagerShimAccessor>(accessor => accessor.Instance == snapshotManager.Object);
            projectResolver = new DefaultProjectResolver(Dispatcher, filePathNormalizer, configurationResolver, snapshotManagerAccessor);

            // Act
            var project = projectResolver.GetMiscellaneousProject();

            // Assert
            Assert.Single(projects);
            Assert.Equal(projectResolver._miscellaneousHostProject.FilePath, project.FilePath);
        }

        private DefaultProjectResolver CreateProjectResolver(Func<ProjectSnapshotShim[]> projectFactory)
        {
            var filePathNormalizer = new FilePathNormalizer();
            var configurationResolver = Mock.Of<RazorConfigurationResolver>(resolver => resolver.Default == RazorConfiguration.Default);
            var snapshotManager = new Mock<ProjectSnapshotManagerShim>();
            snapshotManager.Setup(manager => manager.Projects)
                .Returns(projectFactory);
            snapshotManager.Setup(manager => manager.GetLoadedProject(It.IsAny<string>()))
                .Returns<string>(filePath => projectFactory().FirstOrDefault(project => project.FilePath == filePath));
            var snapshotManagerAccessor = Mock.Of<ProjectSnapshotManagerShimAccessor>(accessor => accessor.Instance == snapshotManager.Object);
            var projectResolver = new DefaultProjectResolver(Dispatcher, filePathNormalizer, configurationResolver, snapshotManagerAccessor);

            return projectResolver;
        }
    }
}
