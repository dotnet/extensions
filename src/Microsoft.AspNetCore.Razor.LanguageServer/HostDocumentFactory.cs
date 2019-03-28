// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class HostDocumentFactory
    {
        public abstract HostDocument Create(string filePath, string targetFilePath);

        public abstract HostDocument Create(string filePath, string targetFilePath, string fileKind);
    }
}
