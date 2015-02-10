// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility
{
    public class DisposableFileSystem : IDisposable
    {
        private readonly bool _automaticCleanup;

        public DisposableFileSystem(bool automaticCleanup = true)
        {
            RootPath = Path.GetTempFileName();
            File.Delete(RootPath);
            Directory.CreateDirectory(RootPath);
            DirectoryInfo = new DirectoryInfoWrapper(new DirectoryInfo(RootPath));
        }

        public string RootPath { get; }

        public DirectoryInfoBase DirectoryInfo { get; }

        public DisposableFileSystem CreateFolder(string path)
        {
            Directory.CreateDirectory(Path.Combine(RootPath, path));
            return this;
        }

        public DisposableFileSystem CreateFile(string path)
        {
            File.WriteAllText(Path.Combine(RootPath, path), "temp");
            return this;
        }

        public DisposableFileSystem CreateFiles(params string[] fileRelativePaths)
        {
            foreach (var path in fileRelativePaths)
            {
                var fullPath = Path.Combine(RootPath, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                File.WriteAllText(
                    fullPath,
                    string.Format("Automatically generated for testing on {0} {1}",
                        DateTime.Now.ToLongDateString(),
                        DateTime.Now.ToLongTimeString()));
            }

            return this;
        }

        public void Dispose()
        {
            if (_automaticCleanup)
            {
                Directory.Delete(RootPath, true);
            }
        }
    }
}