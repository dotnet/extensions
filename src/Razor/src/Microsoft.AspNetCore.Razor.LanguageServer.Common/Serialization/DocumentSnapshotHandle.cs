// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization
{
    internal sealed class DocumentSnapshotHandle
    {
        public DocumentSnapshotHandle(
            string filePath,
            string targetPath,
            string fileKind)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (targetPath == null)
            {
                throw new ArgumentNullException(nameof(targetPath));
            }

            if (fileKind == null)
            {
                throw new ArgumentNullException(nameof(fileKind));
            }

            FilePath = filePath;
            TargetPath = targetPath;
            FileKind = fileKind;
        }

        public string FilePath { get; }

        public string TargetPath { get; }

        public string FileKind { get; }
    }
}
