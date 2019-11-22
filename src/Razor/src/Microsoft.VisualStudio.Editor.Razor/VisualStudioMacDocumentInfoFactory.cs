// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Editor.Razor
{
    /// <summary>
    /// This class is VisualStudio for Mac specific to enable creating an empty document info without having IVT access to Roslyn's types.
    /// </summary>
    internal abstract class VisualStudioMacDocumentInfoFactory
    {
        public abstract DocumentInfo CreateEmpty(string razorFilePath, ProjectId projectId);
    }
}
