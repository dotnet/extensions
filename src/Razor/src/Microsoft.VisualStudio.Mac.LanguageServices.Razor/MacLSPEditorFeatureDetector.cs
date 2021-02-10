// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    [Shared]
    [Export(typeof(LSPEditorFeatureDetector))]
    internal class MacLSPEditorFeatureDetector : LSPEditorFeatureDetector
    {
        public override bool IsLSPEditorAvailable(string documentMoniker, object hierarchy) => false;

        public override bool IsLSPEditorFeatureEnabled() => false;

        public override bool IsLiveShareHost() => false;

        public override bool IsRemoteClient() => false;
    }
}
