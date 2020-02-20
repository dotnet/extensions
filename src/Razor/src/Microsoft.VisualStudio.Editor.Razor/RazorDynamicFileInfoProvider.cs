// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal abstract class RazorDynamicFileInfoProvider
    {
        public abstract void UpdateFileInfo(ProjectSnapshot projectSnapshot, DocumentSnapshot document);

        public abstract void SuppressDocument(ProjectSnapshot project, DocumentSnapshot document);
    }
}
