// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentDocumentResolverTest : TestBase
    {
        [Fact]
        public void TryResolveDocument_AsksPotentialParentProjectForDocumentItsTracking_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshot>();
            var project = Mock.Of<ProjectSnapshot>(shim =>
                shim.GetDocument(normalizedFilePath) == expectedDocument &&
                shim.DocumentFilePaths == new[] { normalizedFilePath });
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.TryResolvePotentialProject(normalizedFilePath, out project) == true);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedDocument, document);
        }

        [Fact]
        public void TryResolveDocument_AsksPotentialParentProjectForDocumentItsNotTracking_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.DocumentFilePaths == new string[0]);
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.TryResolvePotentialProject(normalizedFilePath, out project) == true);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }

        [Fact]
        public void TryResolveDocument_AsksMiscellaneousProjectForDocumentItIsTracking_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshot>();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.GetDocument(normalizedFilePath) == expectedDocument && shim.DocumentFilePaths == new[] { normalizedFilePath });
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.GetMiscellaneousProject() == project);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.True(result);
            Assert.Same(expectedDocument, document);
        }

        [Fact]
        public void TryResolveDocument_AsksMiscellaneousProjectForDocumentItIsNotTracking_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.DocumentFilePaths == new string[0]);
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.GetMiscellaneousProject() == project);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }
    }
}
