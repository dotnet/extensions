// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class HostDocument
    {
        public HostDocument(HostDocument other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            FileKind = other.FileKind;
            FilePath = other.FilePath;
            TargetPath = other.TargetPath;

            GeneratedCodeContainer = new GeneratedCodeContainer();
        }

        public HostDocument(string filePath, string targetPath)
            : this(filePath, targetPath, fileKind: null)
        {
        }

        public HostDocument(string filePath, string targetPath, string fileKind)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (targetPath == null)
            {
                throw new ArgumentNullException(nameof(targetPath));
            }

            FilePath = filePath;
            TargetPath = targetPath;
            FileKind = fileKind ?? FileKinds.GetFileKindFromFilePath(filePath);
            GeneratedCodeContainer = new GeneratedCodeContainer();
        }

        public string FileKind { get; }

        public string FilePath { get; }

        public string TargetPath { get; }

        public GeneratedCodeContainer GeneratedCodeContainer { get; }
    }
}
