// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests
{
    public class FileAbstractionsTests
    {
        [Fact]
        public void TempFolderStartsInitiallyEmpty()
        {
            using (var scenario = new Scenario())
            {
                var contents = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);

                Assert.Equal(Path.GetFileName(scenario.TempFolder), scenario.DirectoryInfo.Name);
                Assert.Equal(scenario.TempFolder, scenario.DirectoryInfo.FullName);
                Assert.Equal(0, contents.Count());
            }
        }


        [Fact]
        public void FilesAreEnumerated()
        {
            using (var scenario = new Scenario()
                .CreateFile("alpha.txt"))
            {
                var contents = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var alphaTxt = contents.OfType<FileInfoBase>().Single();

                Assert.Equal(1, contents.Count());
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        [Fact]
        public void FoldersAreEnumerated()
        {
            using (var scenario = new Scenario()
                .CreateFolder("beta"))
            {
                var contents1 = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);

                Assert.Equal(1, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Equal(0, contents2.Count());
            }
        }

        [Fact]
        public void SubFoldersAreEnumerated()
        {
            using (var scenario = new Scenario()
                .CreateFolder("beta")
                .CreateFile("beta\\alpha.txt"))
            {
                var contents1 = scenario.DirectoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var beta = contents1.OfType<DirectoryInfoBase>().Single();
                var contents2 = beta.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                var alphaTxt = contents2.OfType<FileInfoBase>().Single();

                Assert.Equal(1, contents1.Count());
                Assert.Equal("beta", beta.Name);
                Assert.Equal(1, contents2.Count());
                Assert.Equal("alpha.txt", alphaTxt.Name);
            }
        }

        private class Scenario : IDisposable
        {
            public Scenario()
            {
                TempFolder = Path.GetTempFileName();
                File.Delete(TempFolder);
                Directory.CreateDirectory(TempFolder);
                DirectoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(TempFolder));
            }

            public string TempFolder { get; }

            public DirectoryInfoBase DirectoryInfo { get; }

            public Scenario CreateFolder(string path)
            {
                Directory.CreateDirectory(Path.Combine(TempFolder, path));
                return this;
            }

            public Scenario CreateFile(string path)
            {
                File.WriteAllText(Path.Combine(TempFolder, path), "temp");
                return this;
            }

            public void Dispose()
            {
                Directory.Delete(TempFolder, true);
            }
        }
    }
}