// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RemoteRazorProjectFileSystemTest
    {
        public RemoteRazorProjectFileSystemTest()
        {
            FilePathNormalizer = new FilePathNormalizer();
        }

        private FilePathNormalizer FilePathNormalizer { get; }

        [Fact]
        public void GetItem_RootlessFilePath()
        {
            // Arrange
            var fileSystem = new RemoteRazorProjectFileSystem("/C:/path/to", FilePathNormalizer);
            var documentFilePath = "file.cshtml";

            // Act
            var item = fileSystem.GetItem(documentFilePath, fileKind: null);

            // Assert
            Assert.Equal(documentFilePath, item.FilePath);
            Assert.Equal("/C:/path/to/file.cshtml", item.PhysicalPath);
        }

        [Fact]
        public void GetItem_RootedFilePath_BelongsToProject()
        {
            // Arrange
            var fileSystem = new RemoteRazorProjectFileSystem("/C:/path/to", FilePathNormalizer);
            var documentFilePath = "/C:/path/to/file.cshtml";

            // Act
            var item = fileSystem.GetItem(documentFilePath, fileKind: null);

            // Assert
            Assert.Equal("file.cshtml", item.FilePath);
            Assert.Equal(documentFilePath, item.PhysicalPath);
        }

        [Fact]
        public void GetItem_RootedFilePath_DoesNotBelongToProject()
        {
            // Arrange
            var fileSystem = new RemoteRazorProjectFileSystem("/C:/path/to", FilePathNormalizer);
            var documentFilePath = "/C:/otherpath/to/file.cshtml";

            // Act
            var item = fileSystem.GetItem(documentFilePath, fileKind: null);

            // Assert
            Assert.Equal(documentFilePath, item.FilePath);
            Assert.Equal(documentFilePath, item.PhysicalPath);
        }
    }
}
