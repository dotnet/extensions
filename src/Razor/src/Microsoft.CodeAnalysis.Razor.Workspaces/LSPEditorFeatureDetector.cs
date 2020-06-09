// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    internal abstract class LSPEditorFeatureDetector
    {
        public abstract bool IsLSPEditorAvailable(string documentMoniker, object hierarchy);

        public abstract bool IsRemoteClient();

        public abstract bool IsLSPEditorFeatureEnabled();
    }
}
