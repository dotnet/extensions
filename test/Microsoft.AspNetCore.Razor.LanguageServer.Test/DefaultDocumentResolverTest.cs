// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentDocumentResolverTest
    {
        [Fact]
        public void TryResolveDocument_AsksResolvedParentProjectForDocument_ReturnsTrue()
        {
            // Arrange
            var documentPath = "C:\\path\\to\\document.cshtml";
            var normalizedPath = "C:/path/to/document.cshtml";
            var foregroundDispatcher = Mock.Of<ForegroundDispatcherShim>();
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshotShim>();
            var project = Mock.Of<ProjectSnapshotShim>(shim => shim.GetDocument(normalizedPath) == expectedDocument);
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.TryResolveProject(normalizedPath, out project) == true);
            var documentResolver = new DefaultDocumentResolver(foregroundDispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentPath, out var document);

            // Assert
            Assert.True(result);
            Assert.Same(expectedDocument, document);
        }

        [Fact]
        public void TryResolveDocument_AsksMiscellaneousProjectForDocumentItIsTracking_ReturnsTrue()
        {
            // Arrange
            var documentPath = "C:\\path\\to\\document.cshtml";
            var normalizedPath = "C:/path/to/document.cshtml";
            var foregroundDispatcher = Mock.Of<ForegroundDispatcherShim>();
            var filePathNormalizer = new FilePathNormalizer();
            var expectedDocument = Mock.Of<DocumentSnapshotShim>();
            var project = Mock.Of<ProjectSnapshotShim>(shim => shim.GetDocument(normalizedPath) == expectedDocument && shim.DocumentFilePaths == new[] { normalizedPath });
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.GetMiscellaneousProject() == project);
            var documentResolver = new DefaultDocumentResolver(foregroundDispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentPath, out var document);

            // Assert
            Assert.True(result);
            Assert.Same(expectedDocument, document);
        }

        [Fact]
        public void TryResolveDocument_AsksMiscellaneousProjectForDocumentItIsNotTracking_ReturnsFalse()
        {
            // Arrange
            var documentPath = "C:\\path\\to\\document.cshtml";
            var foregroundDispatcher = Mock.Of<ForegroundDispatcherShim>();
            var filePathNormalizer = new FilePathNormalizer();
            var project = Mock.Of<ProjectSnapshotShim>(shim => shim.DocumentFilePaths == new string[0]);
            var projectResolver = Mock.Of<ProjectResolver>(resolver => resolver.GetMiscellaneousProject() == project);
            var documentResolver = new DefaultDocumentResolver(foregroundDispatcher, projectResolver, filePathNormalizer);

            // Act
            var result = documentResolver.TryResolveDocument(documentPath, out var document);

            // Assert
            Assert.False(result);
            Assert.Null(document);
        }
    }
}
