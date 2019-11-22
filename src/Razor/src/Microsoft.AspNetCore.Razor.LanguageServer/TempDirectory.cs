// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class TempDirectory : IDisposable
    {
        public static readonly TempDirectory Instance = Create();

        private static TempDirectory Create()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(directoryPath);
            return new TempDirectory(directoryPath);
        }

        private TempDirectory(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }

        ~TempDirectory()
        {
            Dispose();
        }
    }
}
