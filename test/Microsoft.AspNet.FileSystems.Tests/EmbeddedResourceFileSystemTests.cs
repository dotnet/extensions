// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.FileSystems
{
    public class EmbeddedResourceFileSystemTests
    {
        [Fact]
        public void When_TryGetFileInfo_and_resource_does_not_exist_then_should_not_get_file_info()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");

            IFileInfo fileInfo;
            provider.TryGetFileInfo("/DoesNotExist.Txt", out fileInfo).ShouldBe(false);

            fileInfo.ShouldBe(null);
        }

        [Fact]
        public void When_TryGetFileInfo_and_resource_exists_in_root_then_should_get_file_info()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");

            IFileInfo fileInfo;
            provider.TryGetFileInfo("/File.txt", out fileInfo).ShouldBe(true);

            fileInfo.ShouldNotBe(null);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBeGreaterThan(0);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("File.txt");
        }

        [Fact]
        public void When_TryGetFileInfo_and_resource_exists_in_subdirectory_then_should_get_file_info()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "Resources");

            IFileInfo fileInfo;
            provider.TryGetFileInfo("/ResourcesInSubdirectory/File3.txt", out fileInfo).ShouldBe(true);

            fileInfo.ShouldNotBe(null);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBeGreaterThan(0);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("ResourcesInSubdirectory/File3.txt");
        }

        [Fact]
        public void When_TryGetFileInfo_and_resources_in_path_then_should_get_file_infos()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");

            IFileInfo fileInfo;
            provider.TryGetFileInfo("/Resources/File.txt", out fileInfo).ShouldBe(true);

            fileInfo.ShouldNotBe(null);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBeGreaterThan(0);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("Resources/File.txt");
        }

        [Fact]
        public void TryGetDirInfo_with_slash()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "Resources");

            IEnumerable<IFileInfo> files;
            provider.TryGetDirectoryContents("/", out files).ShouldBe(true);
            files.Count().ShouldBe(2);

            provider.TryGetDirectoryContents("/file", out files).ShouldBe(false);
            provider.TryGetDirectoryContents("/file/", out files).ShouldBe(false);
            provider.TryGetDirectoryContents("/file.txt", out files).ShouldBe(false);
            provider.TryGetDirectoryContents("/file/txt", out files).ShouldBe(false);
        }

        [Fact]
        public void TryGetDirInfo_without_slash()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");

            IEnumerable<IFileInfo> files;
            provider.TryGetDirectoryContents(string.Empty, out files).ShouldBe(false);
            provider.TryGetDirectoryContents("file", out files).ShouldBe(false);
            provider.TryGetDirectoryContents("file.txt", out files).ShouldBe(false);
        }

        [Fact]
        public void TryGetDirInfo_with_no_matching_base_namespace()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "Unknown.Namespace");

            IEnumerable<IFileInfo> files;
            provider.TryGetDirectoryContents(string.Empty, out files).ShouldBe(false);
            provider.TryGetDirectoryContents("/", out files).ShouldBe(true);
            files.Count().ShouldBe(0);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("path-without-slash")]
        public void TryGetParentPath_ReturnsFalseIfPathDoesNotStartWithSlash(string subpath)
        {
            // Arrange
            var provider = new EmbeddedResourceFileSystem(GetType().Assembly, "BaseNamespace");

            // Act and Assert
            provider.TryGetParentPath(subpath, out var parentPath).ShouldBe(false);
        }

        [Fact]
        public void TryGetParentPath_ReturnsFalseForPathThatIsSlash()
        {
            // Arrange
            var provider = new EmbeddedResourceFileSystem(GetType().Assembly, "BaseNamespace");

            // Act and Assert
            provider.TryGetParentPath("/", out var parentPath).ShouldBe(false);
        }

        [Theory]
        [InlineData("/foo")]
        [InlineData("/bar.resx")]
        public void TryGetParentPath_ReturnsSlashForAllPathsThatStartWithSlash(string subpath)
        {
            // Arrange
            var provider = new EmbeddedResourceFileSystem(GetType().Assembly, "BaseNamespace");

            // Act and Assert
            provider.TryGetParentPath(subpath, out var parentPath).ShouldBe(true);
            parentPath.ShouldBe("/");
        }
    }
}
