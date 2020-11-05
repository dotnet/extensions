// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentDocumentResolverTest : LanguageServerTestBase
    {
        [Fact]
        public void TryResolveDocument_AsksPotentialParentProjectForDocumentItsTracking_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshot>();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.GetDocument(normalizedFilePath) == expectedDocument);
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.TryResolveProject(normalizedFilePath, out project, true) == true);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedDocument, document);
        }

        [Fact]
        public void TryResolveDocument_AsksMiscellaneousProjectForDocumentItIsTracking_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshot>();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.GetDocument(normalizedFilePath) == expectedDocument);
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.TryResolveProject(normalizedFilePath, out project, true) == true);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.True(result);
            Assert.Same(expectedDocument, document);
        }

        [Fact]
        public void TryResolveDocument_AsksPotentialParentProjectForDocumentItsNotTrackingAndMiscellaneousProjectIsNotTrackingEither_ReturnsFalse()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.DocumentFilePaths == Array.Empty<string>());
            var miscProject = Mock.Of<ProjectSnapshot>(shim => shim.DocumentFilePaths == Array.Empty<string>());
            ProjectSnapshot noProject = null;
            var projectResolver = Mock.Of<ProjectResolver>(resolver =>
                resolver.TryResolveProject(normalizedFilePath, out noProject, true) == false);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }

        [Fact]
        public void TryResolveDocument_AsksPotentialParentProjectForDocumentItsNotTrackingButMiscellaneousProjectIs_ReturnsTrue()
        {
            // Arrange
            var documentFilePath = "C:\\path\\to\\document.cshtml";
            var normalizedFilePath = "C:/path/to/document.cshtml";
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshot>();
            var project = Mock.Of<ProjectSnapshot>(shim => shim.DocumentFilePaths == Array.Empty<string>());
            var miscProject = Mock.Of<ProjectSnapshot>(shim => shim.GetDocument(normalizedFilePath) == expectedDocument);
            var projectResolver = Mock.Of<ProjectResolver>(resolver =>
                resolver.TryResolveProject(normalizedFilePath, out miscProject, true) == true);
            var documentResolver = new DefaultDocumentResolver(Dispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentFilePath, out var document);

            // Assert
            Assert.True(result);
            Assert.Same(expectedDocument, document);
        }
    }
}
