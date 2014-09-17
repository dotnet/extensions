// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.FileSystems
{
    public class PhysicalFileSystemTests
    {
        [Fact]
        public void ExistingFilesReturnTrue()
        {
            var provider = new PhysicalFileSystem(Environment.CurrentDirectory);
            var info = provider.GetFileInfo("File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            info.IsReadOnly.ShouldBe(false);

            info = provider.GetFileInfo("/File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            info.IsReadOnly.ShouldBe(false);
        }

        [Fact]
        public void ModifyContent_And_Delete_File_Succeeds()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "OldContent");
            var provider = new PhysicalFileSystem(Path.GetTempPath());
            var fileInfo = provider.GetFileInfo(fileName);
            fileInfo.Length.ShouldBe(new FileInfo(fileInfo.PhysicalPath).Length);
            fileInfo.Exists.ShouldBe(true);

            // Write new content.
            var newData = Encoding.UTF8.GetBytes("OldContent + NewContent");
            fileInfo.WriteContent(newData);
            fileInfo.Exists.ShouldBe(true);
            fileInfo.Length.ShouldBe(newData.Length);

            // Delete the file and verify file info is updated.
            fileInfo.Delete();
            fileInfo.Exists.ShouldBe(false);
            new FileInfo(fileLocation).Exists.ShouldBe(false);
        }

        [Fact]
        public void MissingFilesReturnFalse()
        {
            var provider = new PhysicalFileSystem(Environment.CurrentDirectory);
            var info = provider.GetFileInfo("File5.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);
        }

        [Fact]
        public void SubPathActsAsRoot()
        {
            var provider = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, "sub"));
            var info = provider.GetFileInfo("File2.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
        }

        [Fact]
        public void GetDirectoryContents_FromRootPath_ForEmptyDirectoryName()
        {
            var provider = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, "sub"));
            var info = provider.GetDirectoryContents(string.Empty);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            var firstDirectory = info.Where(f => f.IsDirectory).Where(f => f.Exists).FirstOrDefault();
            Should.Throw<InvalidOperationException>(() => firstDirectory.CreateReadStream());
            Should.Throw<InvalidOperationException>(() => firstDirectory.WriteContent(new byte[10]));
            Should.Throw<NotSupportedException>(() => firstDirectory.CreateFileChangeTrigger());

            var fileInfo = info.Where(f => f.Name == "File2.txt").FirstOrDefault();
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(true);
        }

        [Fact]
        public void NotFoundFileInfo_BasicTests()
        {
            var info = new NotFoundFileInfo("NotFoundFile.txt");
            Should.Throw<InvalidOperationException>(() => info.CreateReadStream());
            Should.Throw<InvalidOperationException>(() => info.WriteContent(new byte[10]));
            Should.Throw<InvalidOperationException>(() => info.Delete());
            Should.Throw<InvalidOperationException>(() => info.CreateFileChangeTrigger());
        }

        [Fact]
        public void RelativePathPastRootNotAllowed()
        {
            var serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));

            var provider = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, "sub"));

            var info = provider.GetFileInfo("..\\File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            info = provider.GetFileInfo(".\\..\\File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            info = provider.GetFileInfo("File2.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            info.PhysicalPath.ShouldBe(Path.Combine(appEnvironment.ApplicationBasePath, "sub", "File2.txt"));
        }

        [Fact]
        public void AbsolutePathNotAllowed()
        {
            var serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));

            var provider = new PhysicalFileSystem(Path.Combine(Environment.CurrentDirectory, "sub"));

            var applicationBase = appEnvironment.ApplicationBasePath;
            var file1 = Path.Combine(applicationBase, "File.txt");
            
            var info = provider.GetFileInfo(file1);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            var file2 = Path.Combine(applicationBase, "sub", "File2.txt");
            info = provider.GetFileInfo(file2);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            var directory1 = Path.Combine(applicationBase, "sub");
            var directoryContents = provider.GetDirectoryContents(directory1);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            var directory2 = Path.Combine(applicationBase, "Does_Not_Exists");
            directoryContents = provider.GetDirectoryContents(directory2);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);
        }
    }
}