// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal abstract class LSPEditorFeatureDetector
    {
        public abstract bool IsLSPEditorAvailable(string documentMoniker, IVsHierarchy hierarchy);

        public abstract bool IsRemoteClient();
    }
}
