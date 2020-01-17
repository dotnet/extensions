// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorFileSynchronizerTest : LanguageServerTestBase
    {
        [Fact]
        public void RazorFileChanged_Added_AddsRazorDocument()
        {
            // Arrange
            var filePath = "/path/to/file.razor";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddDocument(filePath)).Verifiable();
            var synchronizer = new RazorFileSynchronizer(Dispatcher, projectService.Object);

            // Act
            synchronizer.RazorFileChanged(filePath, RazorFileChangeKind.Added);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void RazorFileChanged_Added_AddsCSHTMLDocument()
        {
            // Arrange
            var filePath = "/path/to/file.cshtml";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.AddDocument(filePath)).Verifiable();
            var synchronizer = new RazorFileSynchronizer(Dispatcher, projectService.Object);

            // Act
            synchronizer.RazorFileChanged(filePath, RazorFileChangeKind.Added);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void RazorFileChanged_Removed_RemovesRazorDocument()
        {
            // Arrange
            var filePath = "/path/to/file.razor";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.RemoveDocument(filePath)).Verifiable();
            var synchronizer = new RazorFileSynchronizer(Dispatcher, projectService.Object);

            // Act
            synchronizer.RazorFileChanged(filePath, RazorFileChangeKind.Removed);

            // Assert
            projectService.VerifyAll();
        }

        [Fact]
        public void RazorFileChanged_Removed_RemovesCSHTMLDocument()
        {
            // Arrange
            var filePath = "/path/to/file.cshtml";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.RemoveDocument(filePath)).Verifiable();
            var synchronizer = new RazorFileSynchronizer(Dispatcher, projectService.Object);

            // Act
            synchronizer.RazorFileChanged(filePath, RazorFileChangeKind.Removed);

            // Assert
            projectService.VerifyAll();
        }
    }
}
