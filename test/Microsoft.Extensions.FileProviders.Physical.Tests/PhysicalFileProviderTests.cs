// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public class PhysicalFileProviderTests
    {
        private const int WaitTimeForTokenToFire = 2 * 100;

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForNullPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(null);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForEmptyPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(string.Empty);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes_Windows(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(path);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        public void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes_Unix(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(path);
        }

        public void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(path);
                Assert.IsType(typeof(PhysicalFileInfo), info);
            }
        }

        [Theory]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Windows(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(path);
        }

        [Theory]
        [InlineData("/\0/")]
        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Unix(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(path);
        }

        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(path);
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        public static TheoryData<string> InvalidPaths
        {
            get
            {
                return new TheoryData<string>
                {
                    Path.Combine(". .", "file"),
                    Path.Combine(" ..", "file"),
                    Path.Combine(".. ", "file"),
                    Path.Combine(" .", "file"),
                    Path.Combine(". ", "file"),
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidPaths))]
        public void GetFileInfoReturnsNonExistentFileInfoForIllegalPath(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(path);
                Assert.False(info.Exists);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForAbsolutePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathAboveRootPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(Path.Combine("..", Guid.NewGuid().ToString()));
                Assert.IsType(typeof(NotFoundFileInfo), info);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathThatNavigatesAboveRoot()
        {
            using (var root = new DisposableFileSystem())
            {
                File.Create(Path.Combine(root.RootPath, "b"));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var info = provider.GetFileInfo(Path.Combine("a", "..", "..", root.DirectoryInfo.Name, "b"));
                    Assert.IsType(typeof(NotFoundFileInfo), info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathWithEmptySegmentsThatNavigates()
        {
            using (var root = new DisposableFileSystem())
            {
                File.Create(Path.Combine(root.RootPath, "b"));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var info = provider.GetFileInfo("a///../../" + root.DirectoryInfo.Name + "/b");
                    Assert.IsType(typeof(NotFoundFileInfo), info);
                }
            }
        }

        [Fact]
        public void CreateReadStreamSucceedsOnEmptyFile()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);
                    File.WriteAllBytes(filePath, new byte[0]);
                    var info = provider.GetFileInfo(fileName);
                    using (var stream = info.CreateReadStream())
                    {
                        Assert.NotNull(stream);
                    }
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForHiddenFile()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);
                    File.Create(filePath);
                    var fileInfo = new FileInfo(filePath);
                    File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.Hidden);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType(typeof(NotFoundFileInfo), info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForSystemFile()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);
                    File.Create(filePath);
                    var fileInfo = new FileInfo(filePath);
                    File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.System);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType(typeof(NotFoundFileInfo), info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForFileNameStartingWithPeriod()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = "." + Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType(typeof(NotFoundFileInfo), info);
                }
            }
        }

        [Fact]
        public void TokenIsSameForSamePath()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileInfo = provider.GetFileInfo(fileName);

                    var token1 = provider.Watch(fileName);
                    var token2 = provider.Watch(fileName);

                    Assert.NotNull(token1);
                    Assert.NotNull(token2);
                    Assert.Equal(token2, token1);
                }
            }
        }

        [Fact]
        public async Task TokensFiredOnFileChange()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenCallbackInvokedOnFileChange()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            bool callbackInvoked = false;
                            token.RegisterChangeCallback(state =>
                            {
                                callbackInvoked = true;
                            }, state: null);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(callbackInvoked);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task WatcherWithPolling_ReturnsTrueForFileChangedWhenFileSystemWatcherDoesNotRaiseEvents()
        {
            if (TestPlatformHelper.IsMono)
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.RootPath, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var token = provider.Watch(fileName);
                            File.WriteAllText(fileLocation, "some-content");
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task WatcherWithPolling_ReturnsTrueForFileRemovedWhenFileSystemWatcherDoesNotRaiseEvents()
        {
            if (TestPlatformHelper.IsMono)
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.RootPath, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            root.CreateFile(fileName);
                            var token = provider.Watch(fileName);
                            File.Delete(fileLocation);

                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task WatcherWithPolling_ReturnsTrueForChangedFileWhenQueriedMultipleTimes()
        {
            if (TestPlatformHelper.IsMono)
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.RootPath, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            root.CreateFile(fileName);
                            var token = provider.Watch(fileName);
                            File.Delete(fileLocation);

                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokensFiredOnFileDeleted()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            fileSystemWatcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        // On Unix the minimum invalid file path characters are / and \0
        [Theory]
        [InlineData("/test:test")]
        [InlineData("/dir/name\"")]
        [InlineData("/dir>/name")]
        public void InvalidPath_DoesNotThrowWindows_GetFileInfo(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            InvalidPath_DoesNotThrowGeneric_GetFileInfo(path);
        }

        [Theory]
        [InlineData("/test:test\0")]
        [InlineData("/dir/\0name\"")]
        [InlineData("/dir>/name\0")]
        public void InvalidPath_DoesNotThrowUnix_GetFileInfo(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            InvalidPath_DoesNotThrowGeneric_GetFileInfo(path);
        }

        public void InvalidPath_DoesNotThrowGeneric_GetFileInfo(string path)
        {
            using (var provider = new PhysicalFileProvider(Directory.GetCurrentDirectory()))
            {
                var info = provider.GetFileInfo(path);
                Assert.NotNull(info);
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        [Theory]
        [InlineData("/test:test")]
        [InlineData("/dir/name\"")]
        [InlineData("/dir>/name")]
        public void InvalidPath_DoesNotThrowWindows_GetDirectoryContents(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(path);
        }

        [Theory]
        [InlineData("/test:test\0")]
        [InlineData("/dir/\0name\"")]
        [InlineData("/dir>/name\0")]
        public void InvalidPath_DoesNotThrowUnix_GetDirectoryContents(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(path);
        }

        public void InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(string path)
        {
            using (var provider = new PhysicalFileProvider(Directory.GetCurrentDirectory()))
            {
                var info = provider.GetDirectoryContents(path);
                Assert.NotNull(info);
                Assert.IsType<NotFoundDirectoryContents>(info);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForNullPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(null);
                Assert.IsType(typeof(NotFoundDirectoryContents), contents);
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes_Windows(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(path);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        public void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes_Unix(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(path);
        }

        public void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(path);
                Assert.IsType<PhysicalDirectoryContents>(contents);
            }
        }

        [Theory]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        [MemberData(nameof(InvalidPaths))]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath_Windows(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(path);
        }

        [Theory]
        [InlineData("/\0/")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        [MemberData(nameof(InvalidPaths))]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath_Unix(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(path);
        }

        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(path);
                Assert.IsType(typeof(NotFoundDirectoryContents), contents);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForAbsolutePath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
                Assert.IsType(typeof(NotFoundDirectoryContents), contents);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForNonExistingDirectory()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(Guid.NewGuid().ToString());
                Assert.IsType(typeof(NotFoundDirectoryContents), contents);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsRootDirectoryContentsForEmptyPath()
        {
            using (var root = new DisposableFileSystem())
            {
                File.Create(Path.Combine(root.RootPath, "File" + Guid.NewGuid().ToString()));
                Directory.CreateDirectory(Path.Combine(root.RootPath, "Dir" + Guid.NewGuid().ToString()));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(string.Empty);
                    Assert.Collection(contents.OrderBy(c => c.Name),
                        item => Assert.IsType<PhysicalDirectoryInfo>(item),
                        item => Assert.IsType<PhysicalFileInfo>(item));
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForPathThatNavigatesAboveRoot()
        {
            using (var root = new DisposableFileSystem())
            {
                Directory.CreateDirectory(Path.Combine(root.RootPath, "b"));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(Path.Combine("a", "..", "..", root.DirectoryInfo.Name, "b"));
                    Assert.IsType(typeof(NotFoundDirectoryContents), contents);
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsDoesNotReturnFileInfoForHiddenFile()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);
                var fileInfo = new FileInfo(filePath);
                File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.Hidden);

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsDoesNotReturnFileInfoForSystemFile()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);
                var fileInfo = new FileInfo(filePath);
                File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.System);

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsDoesNotReturnFileInfoForFileNameStartingWithPeriod()
        {
            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = "." + Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [Fact]
        public async Task FileChangeTokenNotNotifiedAfterExpiry()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var changeToken = provider.Watch(fileName);
                            var invocationCount = 0;
                            changeToken.RegisterChangeCallback(_ => { invocationCount++; }, null);

                            // Callback expected.
                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            // Callback not expected.
                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.Equal(1, invocationCount);
                        }
                    }
                }
            }
        }

        [Fact]
        public void TokenIsSameForSamePathCaseInsensitive()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var token = provider.Watch(fileName);
                    var lowerCaseToken = provider.Watch(fileName.ToLowerInvariant());
                    Assert.Equal(token, lowerCaseToken);
                }
            }
        }

        [Fact]
        public async Task CorrectTokensFiredForMultipleFiles()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var fileName1 = Guid.NewGuid().ToString();
                            var token1 = provider.Watch(fileName1);
                            var fileName2 = Guid.NewGuid().ToString();
                            var token2 = provider.Watch(fileName2);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName1));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token1.HasChanged);
                            Assert.False(token2.HasChanged);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName2));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token2.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenNotAffectedByExceptions()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(fileName);

                            token.RegisterChangeCallback(_ =>
                            {
                                throw new Exception();
                            }, null);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForNullFilter()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch(null);

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForFilterThatNavigatesAboveRoot()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch(Path.Combine("a", "..", "..", root.DirectoryInfo.Name, "b"));

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public void TokenForEmptyFilter()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch(string.Empty);

                    Assert.False(token.HasChanged);
                    Assert.True(token.ActiveChangeCallbacks);
                }
            }
        }

        [Fact]
        public void TokenForWhitespaceFilters()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch("  ");

                    Assert.False(token.HasChanged);
                    Assert.True(token.ActiveChangeCallbacks);
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForAbsolutePathFilters()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var path = Path.Combine(root.RootPath, Guid.NewGuid().ToString());
                    var token = provider.Watch(path);

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public async Task TokenFiredOnCreation()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var name = Guid.NewGuid().ToString();
                            var token = provider.Watch(name);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Created, root.RootPath, name));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenFiredOnDeletion()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var name = Guid.NewGuid().ToString();
                            var token = provider.Watch(name);

                            fileSystemWatcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, root.RootPath, name));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenFiredForFilesUnderPathEndingWithSlash()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var directoryName = Guid.NewGuid().ToString();
                            root.CreateFolder(directoryName)
                                .CreateFile(Path.Combine(directoryName, "some-file"));
                            var newDirectory = Path.GetRandomFileName();

                            var token = provider.Watch(directoryName + Path.DirectorySeparatorChar);

                            Directory.Move(
                                Path.Combine(root.RootPath, directoryName),
                                Path.Combine(root.RootPath, newDirectory));

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(
                                WatcherChangeTypes.Renamed,
                                root.RootPath,
                                newDirectory,
                                directoryName));

                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public async Task TokenFiredForRelativePathStartingWithSlash_Windows(string slashes)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            await TokenFiredForRelativePathStartingWithSlash(slashes);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        public async Task TokenFiredForRelativePathStartingWithSlash_Unix(string slashes)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            await TokenFiredForRelativePathStartingWithSlash(slashes);
        }

        public async Task TokenFiredForRelativePathStartingWithSlash(string slashes)
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(slashes + fileName);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        public async Task TokenNotFiredForInvalidPathStartingWithSlash_Windows(string slashes)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            await TokenNotFiredForInvalidPathStartingWithSlash(slashes);
        }

        [Theory]
        [InlineData("/\0/")]
        public async Task TokenNotFiredForInvalidPathStartingWithSlash_Unix(string slashes)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            await TokenNotFiredForInvalidPathStartingWithSlash(slashes);
        }

        public async Task TokenNotFiredForInvalidPathStartingWithSlash(string slashes)
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(slashes + fileName);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.IsType<NullChangeToken>(token);
                            Assert.False(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenFiredForGlobbingPatternsPointingToSubDirectory()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var subDirectoryName = Guid.NewGuid().ToString();
                            var subSubDirectoryName = Guid.NewGuid().ToString();
                            var fileName = Guid.NewGuid().ToString() + ".cshtml";

                            root.CreateFolder(subDirectoryName)
                                .CreateFolder(Path.Combine(subDirectoryName, subSubDirectoryName))
                                .CreateFile(Path.Combine(subDirectoryName, subSubDirectoryName, fileName));

                            var pattern = string.Format(Path.Combine(subDirectoryName, "**", "*.cshtml"));
                            var token = provider.Watch(pattern);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.Combine(root.RootPath, subDirectoryName, subSubDirectoryName), fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public void TokensWithForwardAndBackwardSlashesAreSame()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token1 = provider.Watch(@"a/b\c");
                    var token2 = provider.Watch(@"a\b/c");

                    Assert.Equal(token1, token2);
                }
            }
        }

        [Fact]
        public async Task TokensFiredForOldAndNewNamesOnRename()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var oldFileName = Guid.NewGuid().ToString();
                            var oldToken = provider.Watch(oldFileName);

                            var newFileName = Guid.NewGuid().ToString();
                            var newToken = provider.Watch(newFileName);

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.RootPath, newFileName, oldFileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(oldToken.HasChanged);
                            Assert.True(newToken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokensFiredForNewDirectoryContentsOnRename()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var oldDirectoryName = Guid.NewGuid().ToString();
                            var oldSubDirectoryName = Guid.NewGuid().ToString();
                            var oldSubDirectoryPath = Path.Combine(oldDirectoryName, oldSubDirectoryName);
                            var oldFileName = Guid.NewGuid().ToString();
                            var oldFilePath = Path.Combine(oldDirectoryName, oldSubDirectoryName, oldFileName);

                            var newDirectoryName = Guid.NewGuid().ToString();
                            var newSubDirectoryName = Guid.NewGuid().ToString();
                            var newSubDirectoryPath = Path.Combine(newDirectoryName, newSubDirectoryName);
                            var newFileName = Guid.NewGuid().ToString();
                            var newFilePath = Path.Combine(newDirectoryName, newSubDirectoryName, newFileName);

                            Directory.CreateDirectory(Path.Combine(root.RootPath, newDirectoryName));
                            Directory.CreateDirectory(Path.Combine(root.RootPath, newDirectoryName, newSubDirectoryName));
                            File.Create(Path.Combine(root.RootPath, newDirectoryName, newSubDirectoryName, newFileName));

                            await Task.Delay(WaitTimeForTokenToFire);

                            var oldDirectoryToken = provider.Watch(oldDirectoryName);
                            var oldSubDirectoryToken = provider.Watch(oldSubDirectoryPath);
                            var oldFileToken = provider.Watch(oldFilePath);

                            var newDirectoryToken = provider.Watch(newDirectoryName);
                            var newSubDirectoryToken = provider.Watch(newSubDirectoryPath);
                            var newFileToken = provider.Watch(newFilePath);

                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.False(oldDirectoryToken.HasChanged);
                            Assert.False(oldSubDirectoryToken.HasChanged);
                            Assert.False(oldFileToken.HasChanged);
                            Assert.False(newDirectoryToken.HasChanged);
                            Assert.False(newSubDirectoryToken.HasChanged);
                            Assert.False(newFileToken.HasChanged);

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.RootPath, newDirectoryName, oldDirectoryName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(oldDirectoryToken.HasChanged);
                            Assert.False(oldSubDirectoryToken.HasChanged);
                            Assert.False(oldFileToken.HasChanged);
                            Assert.True(newDirectoryToken.HasChanged);
                            Assert.True(newSubDirectoryToken.HasChanged);
                            Assert.True(newFileToken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenNotFiredForFileNameStartingWithPeriod()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var fileName = "." + Guid.NewGuid().ToString();
                            var token = provider.Watch(Path.GetFileName(fileName));

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.False(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokensNotFiredForHiddenAndSystemFiles()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Temporary work around: https://github.com/aspnet/FileSystem/issues/254.
                return;
            }

            using (var root = new DisposableFileSystem())
            {
                var hiddenFileName = Guid.NewGuid().ToString();
                var hiddenFilePath = Path.Combine(root.RootPath, hiddenFileName);
                File.Create(hiddenFilePath);
                var fileInfo = new FileInfo(hiddenFilePath);
                File.SetAttributes(hiddenFilePath, fileInfo.Attributes | FileAttributes.Hidden);

                var systemFileName = Guid.NewGuid().ToString();
                var systemFilePath = Path.Combine(root.RootPath, systemFileName);
                File.Create(systemFilePath);
                fileInfo = new FileInfo(systemFilePath);
                File.SetAttributes(systemFilePath, fileInfo.Attributes | FileAttributes.System);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var hiddenFiletoken = provider.Watch(Path.GetFileName(hiddenFileName));
                            var systemFiletoken = provider.Watch(Path.GetFileName(systemFileName));

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, hiddenFileName));
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.False(hiddenFiletoken.HasChanged);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, systemFileName));
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.False(systemFiletoken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokensFiredForAllEntriesOnError()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
                        {
                            var token1 = provider.Watch(Guid.NewGuid().ToString());
                            var token2 = provider.Watch(Guid.NewGuid().ToString());
                            var token3 = provider.Watch(Guid.NewGuid().ToString());

                            fileSystemWatcher.CallOnError(new ErrorEventArgs(new Exception()));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token1.HasChanged);
                            Assert.True(token2.HasChanged);
                            Assert.True(token3.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task WildCardToken_RaisesEventsForNewFilesAdded()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.RootPath + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: false))
            using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
            {
                var token = provider.Watch("**/*.txt");
                var directory = Path.Combine(root.RootPath, "subdir1", "subdir2");

                // Act
                fileSystemWatcher.CallOnCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, directory, "a.txt"));
                await Task.Delay(WaitTimeForTokenToFire);

                // Assert
                Assert.True(token.HasChanged);
            }
        }

        [Fact]
        public async Task WildCardToken_RaisesEventsWhenFileSystemWatcherDoesNotFire()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.RootPath + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: true))
            using (var provider = new PhysicalFileProvider(root.RootPath, physicalFilesWatcher))
            {
                var filePath = Path.Combine(root.RootPath, "subdir1", "subdir2", "file.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, "some-content");
                var token = provider.Watch("**/*.txt");
                var compositeToken = Assert.IsType<CompositeFileChangeToken>(token);
                Assert.Equal(2, compositeToken.ChangeTokens.Count);
                var pollingChangeToken = Assert.IsType<PollingWildCardChangeToken>(compositeToken.ChangeTokens[1]);
                pollingChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                // Act
                fileSystemWatcher.EnableRaisingEvents = false;
                File.Delete(filePath);
                await Task.Delay(WaitTimeForTokenToFire);

                // Assert
                Assert.True(token.HasChanged);
            }
        }
    }
}
