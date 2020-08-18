// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    internal abstract class RazorDynamicFileInfoProvider
    {
        public abstract void UpdateLSPFileInfo(Uri documentUri, DynamicDocumentContainer documentContainer);

        public abstract void UpdateFileInfo(string projectFilePath, DynamicDocumentContainer documentContainer);

        public abstract void SuppressDocument(string projectFilePath, string documentFilePath);
    }
}
