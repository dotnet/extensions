// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class RazorDynamicFileInfoProvider
    {
        public abstract void UpdateFileInfo(string projectFilePath, DynamicDocumentContainer documentContainer);

        public abstract void SuppressDocument(string projectFilePath, string documentFilePath);
    }
}
