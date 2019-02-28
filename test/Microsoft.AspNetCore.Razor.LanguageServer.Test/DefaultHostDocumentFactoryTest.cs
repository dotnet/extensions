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
    public class DefaultHostDocumentFactoryTest : TestBase
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
        public void Create_GeneratesProjectRelativeHostDocument()
        {
            // Arrange
            var documentFilePath = "/C:/path/to/file.cshtml";
            var relativeDocumentFilePath = "file.cshtml";
            var projectItem = Mock.Of<RazorProjectItem>(
                pi => pi.PhysicalPath == documentFilePath && pi.FilePath == relativeDocumentFilePath);
            var fileSystem = new Mock<RazorProjectFileSystem>();
            fileSystem.Setup(fs => fs.GetItem(documentFilePath))
                .Returns(projectItem);
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem.Object);
            var projectSnapshot = new Mock<ProjectSnapshot>();
            projectSnapshot.Setup(project => project.GetProjectEngine())
                .Returns(projectEngine);

            // Act
            var hostDocument = Factory.Create(documentFilePath, projectSnapshot.Object);

            // Assert
            Assert.Equal(documentFilePath, hostDocument.FilePath);
            Assert.Equal(relativeDocumentFilePath, hostDocument.TargetPath);
        }
    }
}
