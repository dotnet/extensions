// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.FileSystems
{
    public class EmbeddedResourceFileSystemTests
    {
        [Fact]
        public void When_GetFileInfo_and_resource_does_not_exist_then_should_not_get_file_info()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");

            var fileInfo = provider.GetFileInfo("DoesNotExist.Txt");
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(false);
        }

        [Fact]
        public void When_GetFileInfo_and_resource_exists_in_root_then_should_get_file_info()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");
            var expectedFileLength = new FileInfo("File.txt").Length;
            var fileInfo = provider.GetFileInfo("File.txt");
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(true);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBe(expectedFileLength);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("File.txt");

            //Passing in a leading slash
            fileInfo = provider.GetFileInfo("/File.txt");
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(true);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBe(expectedFileLength);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("File.txt");
        }

        [Fact]
        public void When_GetFileInfo_and_resource_exists_in_subdirectory_then_should_get_file_info()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "Resources");

            var fileInfo = provider.GetFileInfo("ResourcesInSubdirectory/File3.txt");
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(true);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBeGreaterThan(0);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("File3.txt");
        }

        [Fact]
        public void When_GetFileInfo_and_resources_in_path_then_should_get_file_infos()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "");

            var fileInfo = provider.GetFileInfo("Resources/File.txt");
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(true);
            fileInfo.LastModified.ShouldNotBe(default(DateTime));
            fileInfo.Length.ShouldBeGreaterThan(0);
            fileInfo.IsDirectory.ShouldBe(false);
            fileInfo.PhysicalPath.ShouldBe(null);
            fileInfo.Name.ShouldBe("File.txt");
        }

        [Fact]
        public void GetDirectoryContents()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "Resources");

            var files = provider.GetDirectoryContents("");
            files.ShouldNotBe(null);
            files.Count().ShouldBe(2);
            provider.GetDirectoryContents("file").Exists.ShouldBe(false);
            provider.GetDirectoryContents("file/").Exists.ShouldBe(false);
            provider.GetDirectoryContents("file.txt").Exists.ShouldBe(false);
            provider.GetDirectoryContents("file/txt").Exists.ShouldBe(false);
        }

        [Fact]
        public void GetDirInfo_with_no_matching_base_namespace()
        {
            var provider = new EmbeddedResourceFileSystem(this.GetType().Assembly, "Unknown.Namespace");

            var files = provider.GetDirectoryContents(string.Empty);
            files.ShouldNotBe(null);
            files.Exists.ShouldBe(true);
            files.Count().ShouldBe(0);
        }
    }
}