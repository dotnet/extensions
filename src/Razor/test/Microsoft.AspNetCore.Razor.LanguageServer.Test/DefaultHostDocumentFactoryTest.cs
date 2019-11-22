// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using OmniSharp.Extensions.LanguageServer.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultHostDocumentFactoryTest : LanguageServerTestBase
    {
        public DefaultHostDocumentFactoryTest()
        {
            var store = new DefaultGeneratedCodeContainerStore(
                Dispatcher,
                Mock.Of<DocumentVersionCache>(),
                new Lazy<ILanguageServer>(() => null));
            Factory = new DefaultHostDocumentFactory(Dispatcher, store);
        }

        private DefaultHostDocumentFactory Factory { get; }

        [Fact]
        public void Create_NoFileKind_CreatesHostDocumentProperly()
        {
            // Arrange
            var filePath = "/path/to/file.razor";
            var targetPath = "file.razor";

            // Act
            var hostDocument = Factory.Create(filePath, targetPath);

            // Assert
            Assert.Equal(filePath, hostDocument.FilePath);
            Assert.Equal(targetPath, hostDocument.TargetPath);
            Assert.Equal(FileKinds.Component, hostDocument.FileKind);
        }

        [Fact]
        public void Create_WithFileKind_CreatesHostDocumentProperly()
        {
            // Arrange
            var filePath = "/path/to/file.cshtml";
            var targetPath = "file.cshtml";
            var fileKind = FileKinds.Component;

            // Act
            var hostDocument = Factory.Create(filePath, targetPath, fileKind);

            // Assert
            Assert.Equal(filePath, hostDocument.FilePath);
            Assert.Equal(targetPath, hostDocument.TargetPath);
            Assert.Equal(fileKind, hostDocument.FileKind);
        }
    }
}
