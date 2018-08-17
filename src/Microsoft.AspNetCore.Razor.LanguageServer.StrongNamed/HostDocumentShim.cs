// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public abstract class HostDocumentShim
    {
        public abstract string FilePath { get; }

        public abstract string TargetPath { get; }

        public abstract GeneratedCodeContainerShim GeneratedCodeContainer { get; }

        public static HostDocumentShim Create(string filePath, string targetPath)
        {
            var hostDocument = new HostDocument(filePath, targetPath);
            var hostDocumentShim = new DefaultHostDocumentShim(hostDocument);
            return hostDocumentShim;
        }
    }
}
