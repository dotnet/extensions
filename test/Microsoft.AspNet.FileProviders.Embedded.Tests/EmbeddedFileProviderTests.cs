// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.FileProviders.Embedded.Tests
{
    public class EmbeddedFileProviderTests
    {
        private static readonly string Namespace = typeof(EmbeddedFileProviderTests).Namespace;

        [Fact]
        public void When_GetFileInfo_and_resource_does_not_exist_then_should_not_get_file_info()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace);

            var fileInfo = provider.GetFileInfo("DoesNotExist.Txt");
            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.Exists);
        }

        [Fact]
        public void When_GetFileInfo_and_resource_exists_in_root_then_should_get_file_info()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace);
            var expectedFileLength = new FileInfo("File.txt").Length;
            var fileInfo = provider.GetFileInfo("File.txt");
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.Equal(expectedFileLength, fileInfo.Length);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File.txt", fileInfo.Name);

            //Passing in a leading slash
            fileInfo = provider.GetFileInfo("/File.txt");
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.Equal(expectedFileLength, fileInfo.Length);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File.txt", fileInfo.Name);
        }

        [Fact]
        public void When_GetFileInfo_and_resource_exists_in_subdirectory_then_should_get_file_info()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace + ".Resources");

            var fileInfo = provider.GetFileInfo("ResourcesInSubdirectory/File3.txt");
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.True(fileInfo.Length > 0);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File3.txt", fileInfo.Name);
        }

        [Fact]
        public void When_GetFileInfo_and_resources_in_path_then_should_get_file_infos()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace);

            var fileInfo = provider.GetFileInfo("Resources/File.txt");
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
            Assert.NotEqual(default(DateTimeOffset), fileInfo.LastModified);
            Assert.True(fileInfo.Length > 0);
            Assert.False(fileInfo.IsDirectory);
            Assert.Null(fileInfo.PhysicalPath);
            Assert.Equal("File.txt", fileInfo.Name);
        }

        [Fact]
        public void GetDirectoryContents()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace + ".Resources");

            var files = provider.GetDirectoryContents("");
            Assert.NotNull(files);
            Assert.Equal(2, files.Count());
            Assert.False(provider.GetDirectoryContents("file").Exists);
            Assert.False(provider.GetDirectoryContents("file/").Exists);
            Assert.False(provider.GetDirectoryContents("file.txt").Exists);
            Assert.False(provider.GetDirectoryContents("file/txt").Exists);
        }

        [Fact]
        public void GetDirInfo_with_no_matching_base_namespace()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, "Unknown.Namespace");

            var files = provider.GetDirectoryContents(string.Empty);
            Assert.NotNull(files);
            Assert.True(files.Exists);
            Assert.Equal(0, files.Count());
        }

        [Fact]
        public void Trigger_ShouldNot_Support_Registering_Callbacks()
        {
            var provider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, Namespace);
            var trigger = provider.Watch("Resources/File.txt");
            Assert.NotNull(trigger);
            Assert.False(trigger.ActiveExpirationCallbacks);
            Assert.False(trigger.IsExpired);
        }
    }
}