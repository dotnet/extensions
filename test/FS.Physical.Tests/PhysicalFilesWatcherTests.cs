// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Physical.Tests
{
    public class PhysicalFilesWatcherTests
    {
        [Fact]
        public void CreateFileChangeToken_DoesNotAllowPathsAboveRoot()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var token = physicalFilesWatcher.CreateFileChangeToken(Path.GetFullPath(Path.Combine(root.RootPath, "..")));
                Assert.IsType<NullChangeToken>(token);

                token = physicalFilesWatcher.CreateFileChangeToken(Path.GetFullPath(Path.Combine(root.RootPath, "../")));
                Assert.IsType<NullChangeToken>(token);

                token = physicalFilesWatcher.CreateFileChangeToken("..");
                Assert.IsType<NullChangeToken>(token);
            }
        }
    }
}
