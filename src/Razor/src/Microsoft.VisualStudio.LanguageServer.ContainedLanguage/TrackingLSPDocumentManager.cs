// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal abstract class TrackingLSPDocumentManager : LSPDocumentManager
    {
        public abstract void TrackDocument(ITextBuffer buffer);

        public abstract void UntrackDocument(ITextBuffer buffer);

        [Obsolete("Use the int override instead")]
        public abstract void UpdateVirtualDocument<TVirtualDocument>(
            Uri hostDocumentUri,
            IReadOnlyList<ITextChange> changes,
            long hostDocumentVersion) where TVirtualDocument : VirtualDocument;

        public abstract void UpdateVirtualDocument<TVirtualDocument>(
            Uri hostDocumentUri,
            IReadOnlyList<ITextChange> changes,
            int hostDocumentVersion) where TVirtualDocument : VirtualDocument;
    }
}
