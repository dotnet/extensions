// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class OmniSharpHostDocument
    {
        public OmniSharpHostDocument(string filePath, string targetPath, string kind)
        {
            InternalHostDocument = new HostDocument(filePath, targetPath, kind);
        }

        public string FilePath => InternalHostDocument.FilePath;

        public string TargetPath => InternalHostDocument.TargetPath;

        public string FileKind => InternalHostDocument.FileKind;

        internal HostDocument InternalHostDocument { get; }
    }
}
