// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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
            var provider = new PhysicalFileSystem(".");
            IFileInfo info;
            provider.TryGetFileInfo("File.txt", out info).ShouldBe(true);
            info.ShouldNotBe(null);
        }

        [Fact]
        public void MissingFilesReturnFalse()
        {
            var provider = new PhysicalFileSystem(".");
            IFileInfo info;
            provider.TryGetFileInfo("File5.txt", out info).ShouldBe(false);
            info.ShouldBe(null);
        }

        [Fact]
        public void SubPathActsAsRoot()
        {
            var provider = new PhysicalFileSystem("sub");
            IFileInfo info;
            provider.TryGetFileInfo("File2.txt", out info).ShouldBe(true);
            info.ShouldNotBe(null);
        }

        [Fact]
        public void RelativeOrAbsolutePastRootNotAllowed()
        {
            var serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));

            var provider = new PhysicalFileSystem("sub");
            IFileInfo info;
            
            provider.TryGetFileInfo("..\\File.txt", out info).ShouldBe(false);
            info.ShouldBe(null);
            
            provider.TryGetFileInfo(".\\..\\File.txt", out info).ShouldBe(false);
            info.ShouldBe(null);

            var applicationBase = appEnvironment.ApplicationBasePath;
            var file1 = Path.Combine(applicationBase, "File.txt");
            var file2 = Path.Combine(applicationBase, "sub", "File2.txt");
            provider.TryGetFileInfo(file1, out info).ShouldBe(false);
            info.ShouldBe(null);

            provider.TryGetFileInfo(file2, out info).ShouldBe(true);
            info.ShouldNotBe(null);
            info.PhysicalPath.ShouldBe(file2);

            provider.TryGetFileInfo("/File2.txt", out info).ShouldBe(true);
            info.ShouldNotBe(null);
            info.PhysicalPath.ShouldBe(file2);
        }
    }
}
