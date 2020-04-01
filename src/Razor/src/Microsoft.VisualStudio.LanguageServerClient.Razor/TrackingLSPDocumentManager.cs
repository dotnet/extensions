// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal abstract class TrackingLSPDocumentManager : LSPDocumentManager
    {
        public abstract void TrackDocument(ITextBuffer buffer);

        public abstract void UntrackDocument(ITextBuffer buffer);

        public abstract void UpdateVirtualDocument<TVirtualDocument>(
            Uri hostDocumentUri,
            IReadOnlyList<TextChange> changes,
            long hostDocumentVersion,
            bool provisional = false) where TVirtualDocument : VirtualDocument;
    }
}
